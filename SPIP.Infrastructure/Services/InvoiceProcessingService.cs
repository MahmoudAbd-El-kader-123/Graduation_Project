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
    private const decimal CurrencyTolerance = 0.01m;
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
                ConfidenceScore = aiResponse.ConfidenceScore ?? 0,
                ModelUsed = aiResponse.ModelUsed ?? "not-provided"
            });

            await TransitionAsync(invoice, InvoiceStatus.Validated, "StatusChange", "Invoice data validated.", cancellationToken);
            await _reconciliationService.ReconcileAsync(invoiceId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Invoice processing failed for invoice {InvoiceId}", invoiceId);
            var previousStatus = invoice.Status;
            invoice.Status = InvoiceStatus.Failed;
            invoice.ProcessingLogs.Add(CreateLog(previousStatus, InvoiceStatus.Failed, "ProcessingError", ex.Message));
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
        if (!TryParseInvoiceDate(response.InvoiceDate, out _))
            errors.Add("InvoiceDate must use the yyyy-MM-dd format.");
        if (string.IsNullOrWhiteSpace(response.Currency))
            errors.Add("Currency is required.");
        if (response.Subtotal is < 0)
            errors.Add("Subtotal must not be negative.");
        if (response.Vat is < 0)
            errors.Add("Vat must not be negative.");
        if (response.Total <= 0)
            errors.Add("Total must be greater than zero.");
        if (response.Items is null || response.Items.Count == 0)
        {
            errors.Add("At least one invoice item is required.");
            return errors;
        }

        foreach (var item in response.Items)
        {
            if (string.IsNullOrWhiteSpace(item.SupplierSku))
                errors.Add("Each item must include SupplierSku.");
            if (item.Quantity <= 0)
                errors.Add("Each item quantity must be greater than zero.");
            if (item.UnitPrice <= 0)
                errors.Add("Each item unit price must be greater than zero.");
            if (item.Amount <= 0)
                errors.Add("Each item amount must be greater than zero.");
        }

        ValidateInvoiceAmounts(response, errors);
        return errors;
    }

    private static void MapAIResponse(Invoice invoice, AIExtractionResponseDto response)
    {
        TryParseInvoiceDate(response.InvoiceDate, out var invoiceDate);
        invoice.InvoiceNumber = response.InvoiceNumber;
        invoice.VendorName = response.VendorName;
        invoice.InvoiceDate = invoiceDate;
        invoice.Currency = response.Currency!;
        invoice.Subtotal = response.Subtotal ?? response.Items.Sum(i => i.Quantity * i.UnitPrice);
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

    private static void ValidateInvoiceAmounts(AIExtractionResponseDto response, ICollection<string> errors)
    {
        var calculatedSubtotal = response.Items.Sum(item => item.Quantity * item.UnitPrice);
        var grossItemsTotal = response.Items.Sum(item => item.Amount);

        if (response.Subtotal.HasValue &&
            Math.Abs(response.Subtotal.Value - calculatedSubtotal) > CurrencyTolerance)
        {
            errors.Add("Subtotal does not match the sum of quantity multiplied by unit price.");
        }

        if (Math.Abs(response.Total - grossItemsTotal) > CurrencyTolerance)
            errors.Add("Total does not match the sum of VAT-inclusive item amounts.");

        if (response.Subtotal.HasValue &&
            response.Vat.HasValue &&
            Math.Abs(response.Total - (response.Subtotal.Value + response.Vat.Value)) > CurrencyTolerance)
        {
            errors.Add("Total does not match subtotal plus VAT.");
        }
    }

    private static bool TryParseInvoiceDate(string invoiceDate, out DateTime parsedDate) =>
        DateTime.TryParseExact(
            invoiceDate,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out parsedDate);

    private static InvoiceProcessingLog CreateLog(InvoiceStatus? fromStatus, InvoiceStatus toStatus, string eventType, string message) =>
        new()
        {
            FromStatus = fromStatus,
            ToStatus = toStatus,
            EventType = eventType,
            Message = message
        };
}
