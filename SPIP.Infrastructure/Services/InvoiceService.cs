using AutoMapper;
using Hangfire;
using Microsoft.Extensions.Logging;
using SPIP.Application.DTOs.Invoice;
using SPIP.Application.Helpers;
using SPIP.Application.Interfaces.Repositories;
using SPIP.Application.Interfaces.Services;
using SPIP.Application.Interfaces.Storage;
using SPIP.Domain.Entities;
using SPIP.Domain.Enums;
using SPIP.Infrastructure.BackgroundJobs;
using SPIP.Shared.Pagination;
using SPIP.Shared.Result;

namespace SPIP.Infrastructure.Services;

public class InvoiceService : IInvoiceService
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IInvoiceProcessingLogRepository _logRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserRepository _userRepository;
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(
        IInvoiceRepository invoiceRepository,
        IInvoiceProcessingLogRepository logRepository,
        IFileStorageService fileStorageService,
        ICurrentUserService currentUserService,
        IUserRepository userRepository,
        IPurchaseOrderRepository purchaseOrderRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IBackgroundJobClient backgroundJobClient,
        ILogger<InvoiceService> logger)
    {
        _invoiceRepository = invoiceRepository;
        _logRepository = logRepository;
        _fileStorageService = fileStorageService;
        _currentUserService = currentUserService;
        _userRepository = userRepository;
        _purchaseOrderRepository = purchaseOrderRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    public async Task<Result<InvoiceUploadResultDto>> UploadInvoiceAsync(UploadInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        var currentIdentityId = _currentUserService.UserId;
        if (currentIdentityId is null)
            return Result<InvoiceUploadResultDto>.Failure("User not authenticated.");

        var currentUser = await _userRepository.GetByIdentityIdAsync(currentIdentityId.Value);
        if (currentUser is null)
            return Result<InvoiceUploadResultDto>.Failure("User profile not found.");

        var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(request.PurchaseOrderId);
        if (purchaseOrder is null)
            return Result<InvoiceUploadResultDto>.Failure("Purchase Order not found.");

        var extension = Path.GetExtension(request.File.FileName);
        await using var uploadStream = request.File.OpenReadStream();
        if (!FileValidationHelper.ValidateFileSignature(uploadStream, extension))
            return Result<InvoiceUploadResultDto>.Failure("File content does not match the declared type.");

        var contentType = FileValidationHelper.GetContentTypeFromExtension(extension);
        var (storedPath, storedFileName) = await _fileStorageService.SaveFileWithGuidAsync(
            uploadStream,
            request.File.FileName,
            contentType,
            cancellationToken);

        var invoice = new Invoice
        {
            PurchaseOrderId = purchaseOrder.Id,
            VendorId = purchaseOrder.VendorId,
            VendorName = purchaseOrder.Vendor?.Name ?? string.Empty,
            UploadedByUserId = currentUser.Id,
            Status = InvoiceStatus.Uploaded,
            InvoiceDate = DateTime.UtcNow,
            UploadedFiles =
            {
                new UploadedFile
                {
                    FileName = request.File.FileName,
                    OriginalFileName = request.File.FileName,
                    StoredFileName = storedFileName,
                    StoragePath = storedPath,
                    ContentType = contentType,
                    FileSizeBytes = request.File.Length
                }
            },
            ProcessingLogs =
            {
                CreateLog(null, InvoiceStatus.Uploaded, "StatusChange", "Invoice uploaded successfully.")
            }
        };

        await _invoiceRepository.AddAsync(invoice);
        await _unitOfWork.SaveChangesAsync();

        var previousStatus = invoice.Status;
        invoice.Status = InvoiceStatus.Queued;
        invoice.ProcessingLogs.Add(CreateLog(previousStatus, InvoiceStatus.Queued, "StatusChange", "Invoice queued for processing."));
        await _invoiceRepository.UpdateAsync(invoice);
        await _unitOfWork.SaveChangesAsync();

        _backgroundJobClient.Enqueue<InvoiceProcessingJob>(job => job.ProcessAsync(invoice.Id, CancellationToken.None));
        _logger.LogInformation("Invoice {InvoiceId} uploaded and queued by user {UserId}", invoice.Id, currentUser.Id);

        return Result<InvoiceUploadResultDto>.Success(new InvoiceUploadResultDto(invoice.Id, "Queued", request.File.FileName));
    }

    public async Task<Result<InvoiceDetailDto>> GetByIdAsync(int invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceRepository.GetWithDetailsByIdAsync(invoiceId, cancellationToken);
        if (invoice is null)
            return Result<InvoiceDetailDto>.Failure("Invoice not found.");

        if (!await CanAccessInvoiceAsync(invoice))
            return Result<InvoiceDetailDto>.Failure("Access denied.");

        return Result<InvoiceDetailDto>.Success(_mapper.Map<InvoiceDetailDto>(invoice));
    }

    public async Task<Result<(Stream FileStream, string ContentType, string FileName)>> DownloadFileAsync(int invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceRepository.GetWithDetailsByIdAsync(invoiceId, cancellationToken);
        if (invoice is null)
            return Result<(Stream, string, string)>.Failure("Invoice not found.");

        if (!await CanAccessInvoiceAsync(invoice))
            return Result<(Stream, string, string)>.Failure("Access denied.");

        var uploadedFile = invoice.UploadedFiles.FirstOrDefault();
        if (uploadedFile is null)
            return Result<(Stream, string, string)>.Failure("Invoice file not found.");

        var stream = await _fileStorageService.GetFileAsync(uploadedFile.StoragePath);
        return Result<(Stream, string, string)>.Success((stream, uploadedFile.ContentType ?? "application/octet-stream", uploadedFile.OriginalFileName));
    }

    public async Task<Result<PagedResult<InvoiceListItemDto>>> GetAllPagedAsync(InvoiceListParameters parameters, CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(parameters.PageNumber, 1);
        var pageSize = Math.Clamp(parameters.PageSize, 1, 50);
        parameters.PageNumber = pageNumber;
        parameters.PageSize = pageSize;

        var (items, totalCount) = await _invoiceRepository.GetPagedAsync(parameters, cancellationToken);
        var dtos = _mapper.Map<List<InvoiceListItemDto>>(items);

        foreach (var dto in dtos)
        {
            var invoice = items.First(i => i.Id == dto.Id);
            dto.LastError = invoice.ProcessingLogs
                .Where(l => l.ToStatus == InvoiceStatus.Failed || l.EventType.Contains("Error", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => l.Message)
                .FirstOrDefault();
        }

        return Result<PagedResult<InvoiceListItemDto>>.Success(new PagedResult<InvoiceListItemDto>
        {
            Items = dtos,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }

    private async Task<bool> CanAccessInvoiceAsync(Invoice invoice)
    {
        var currentIdentityId = _currentUserService.UserId;
        if (currentIdentityId is null)
            return false;

        var currentUser = await _userRepository.GetByIdentityIdAsync(currentIdentityId.Value);
        return currentUser is not null &&
            (invoice.UploadedByUserId == currentUser.Id ||
             string.Equals(currentUser.RoleName, "Admin", StringComparison.OrdinalIgnoreCase));
    }

    private static InvoiceProcessingLog CreateLog(InvoiceStatus? fromStatus, InvoiceStatus toStatus, string eventType, string message) =>
        new()
        {
            FromStatus = fromStatus,
            ToStatus = toStatus,
            EventType = eventType,
            Message = message
        };
}
