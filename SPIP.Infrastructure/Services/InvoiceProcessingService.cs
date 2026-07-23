using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SPIP.Application.DTOs.AI;
using SPIP.Application.Interfaces.AI;
using SPIP.Application.Interfaces.Repositories;
using SPIP.Application.Interfaces.Services;
using SPIP.Application.Interfaces.Storage;
using SPIP.Domain.Entities;
using SPIP.Domain.Enums;

namespace SPIP.Infrastructure.Services;

public class InvoiceProcessingService : IInvoiceProcessingService
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IAIExtractionService _aiExtractionService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IReconciliationService _reconciliationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<InvoiceProcessingService> _logger;

    public InvoiceProcessingService(
        IInvoiceRepository invoiceRepository,
        IInvoiceProcessingLogRepository logRepository,
        IAIExtractionService aiExtractionService,
        IFileStorageService fileStorageService,
        IReconciliationService reconciliationService,
        IUnitOfWork unitOfWork,
        ILogger<InvoiceProcessingService> logger)
    {
        _invoiceRepository = invoiceRepository;
        _aiExtractionService = aiExtractionService;
        _fileStorageService = fileStorageService;
        _reconciliationService = reconciliationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task ProcessInvoiceAsync(int invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceRepository.GetWithDetailsByIdAsync(invoiceId, cancellationToken)
            ?? throw new InvalidOperationException($"Invoice {invoiceId} was not found.");

        try
        {
            await TransitionAsync(invoice, InvoiceStatus.Processing, "StatusChange", "Invoice processing started.", cancellationToken);

            var uploadedFile = invoice.UploadedFiles.FirstOrDefault()
                ?? throw new InvalidOperationException("Invoice file was not found.");

            await using var fileStream = await _fileStorageService.GetFileAsync(uploadedFile.StoragePath);
            var aiResponse = await _aiExtractionService.ExtractInvoiceDataAsync(fileStream, uploadedFile.OriginalFileName, cancellationToken);

            await TransitionAsync(invoice, InvoiceStatus.Extracted, "AIExtractionComplete", "AI extraction completed.", cancellationToken);

            var validationErrors = ValidateAIResponse(aiResponse);
            if (validationErrors.Count > 0)
            {
                await TransitionAsync(invoice, InvoiceStatus.NeedsReview, "ValidationError", string.Join(" ", validationErrors), cancellationToken);
                return;
            }

            MapAIResponse(invoice, aiResponse);
            invoice.AIExtractionResults.Add(new AIExtractionResult
            {
                RawExtractedJson = JsonSerializer.Serialize(aiResponse),
                ConfidenceScore = 1.0,
                ModelUsed = "external-ai"
            });

            await TransitionAsync(invoice, InvoiceStatus.Validated, "StatusChange", "Invoice data validated.", cancellationToken);
            await _reconciliationService.ReconcileAsync(invoiceId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Invoice processing failed for invoice {InvoiceId}", invoiceId);
            invoice.Status = InvoiceStatus.Failed;
            invoice.ProcessingLogs.Add(CreateLog(invoice.Status, InvoiceStatus.Failed, "ProcessingError", ex.Message));
            await _invoiceRepository.UpdateAsync(invoice);
            await _unitOfWork.SaveChangesAsync();
            throw;
        }
    }

    private async Task TransitionAsync(Invoice invoice, InvoiceStatus toStatus, string eventType, string message, CancellationToken cancellationToken)
    {
        var fromStatus = invoice.Status;
        invoice.Status = toStatus;
        invoice.ProcessingLogs.Add(CreateLog(fromStatus, toStatus, eventType, message));
        await _invoiceRepository.UpdateAsync(invoice);
        await _unitOfWork.SaveChangesAsync();
    }

    private static List<string> ValidateAIResponse(AIExtractionResponseDto response)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(response.VendorName))
            errors.Add("VendorName is required.");
        if (string.IsNullOrWhiteSpace(response.InvoiceNumber))
            errors.Add("InvoiceNumber is required.");
        if (string.IsNullOrWhiteSpace(response.InvoiceDate))
            errors.Add("InvoiceDate is required.");
        if (response.Total <= 0)
            errors.Add("Total must be greater than zero.");
        if (response.Items.Count == 0)
            errors.Add("At least one invoice item is required.");

        foreach (var item in response.Items)
        {
            if (string.IsNullOrWhiteSpace(item.SupplierSku))
                errors.Add("Each item must include SupplierSku.");
            if (item.Quantity <= 0)
                errors.Add("Each item quantity must be greater than zero.");
            if (item.UnitPrice <= 0)
                errors.Add("Each item unit price must be greater than zero.");
        }

        return errors;
    }

    private static void MapAIResponse(Invoice invoice, AIExtractionResponseDto response)
    {
        invoice.InvoiceNumber = response.InvoiceNumber;
        invoice.VendorName = response.VendorName;
        invoice.InvoiceDate = DateTime.TryParseExact(response.InvoiceDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var invoiceDate)
            ? invoiceDate
            : DateTime.UtcNow;
        invoice.Currency = response.Currency ?? "USD";
        invoice.Subtotal = response.Subtotal ?? response.Items.Sum(i => i.Amount);
        invoice.Vat = response.Vat ?? 0;
        invoice.TotalAmount = response.Total;

        invoice.Items.Clear();
        foreach (var item in response.Items)
        {
            invoice.Items.Add(new InvoiceItem
            {
                SupplierSku = item.SupplierSku,
                Description = item.Description ?? string.Empty,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                LineTotal = item.Amount
            });
        }
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
