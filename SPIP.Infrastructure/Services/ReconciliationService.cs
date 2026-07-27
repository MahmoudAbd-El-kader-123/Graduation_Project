using System.Globalization;
using Microsoft.Extensions.Logging;
using SPIP.Application.Interfaces.Repositories;
using SPIP.Application.Interfaces.Services;
using SPIP.Domain.Entities;
using SPIP.Domain.Enums;

namespace SPIP.Infrastructure.Services;

public class ReconciliationService : IReconciliationService
{
    private const decimal AmountTolerance = 0.01m;

    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReconciliationService> _logger;

    public ReconciliationService(
        IInvoiceRepository invoiceRepository,
        IPurchaseOrderRepository purchaseOrderRepository,
        IUnitOfWork unitOfWork,
        ILogger<ReconciliationService> logger)
    {
        _invoiceRepository = invoiceRepository;
        _purchaseOrderRepository = purchaseOrderRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task ReconcileAsync(int invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceRepository.GetWithDetailsByIdAsync(invoiceId, cancellationToken)
            ?? throw new InvalidOperationException($"Invoice {invoiceId} was not found.");

        var purchaseOrder = await _purchaseOrderRepository.GetWithItemsByIdAsync(invoice.PurchaseOrderId);
        if (purchaseOrder is null)
        {
            await TransitionAsync(invoice, InvoiceStatus.NeedsReview, "ValidationError", "Purchase Order not found.");
            return;
        }

        await TransitionAsync(invoice, InvoiceStatus.Compared, "StatusChange", "Invoice comparison started.");

        invoice.Discrepancies.Clear();
        foreach (var invoiceItem in invoice.Items)
        {
            var poItem = purchaseOrder.Items.FirstOrDefault(item =>
                string.Equals(item.Product?.SkuSupplier, invoiceItem.SupplierSku, StringComparison.OrdinalIgnoreCase));

            if (poItem is null)
            {
                invoice.Discrepancies.Add(CreateDiscrepancy(invoice.Id, invoiceItem.Id, DiscrepancyType.MissingSku, "SupplierSku", "N/A", invoiceItem.SupplierSku));
                continue;
            }

            if (invoiceItem.Quantity != poItem.Quantity)
                invoice.Discrepancies.Add(CreateDiscrepancy(invoice.Id, invoiceItem.Id, DiscrepancyType.QuantityMismatch, "Quantity", poItem.Quantity.ToString(CultureInfo.InvariantCulture), invoiceItem.Quantity.ToString(CultureInfo.InvariantCulture)));

            if (invoiceItem.UnitPrice != poItem.UnitPrice)
                invoice.Discrepancies.Add(CreateDiscrepancy(invoice.Id, invoiceItem.Id, DiscrepancyType.UnitPriceMismatch, "UnitPrice", poItem.UnitPrice.ToString(CultureInfo.InvariantCulture), invoiceItem.UnitPrice.ToString(CultureInfo.InvariantCulture)));

            if (Math.Abs(invoiceItem.LineTotal - poItem.Amount) > AmountTolerance)
                invoice.Discrepancies.Add(CreateDiscrepancy(invoice.Id, invoiceItem.Id, DiscrepancyType.AmountMismatch, "Amount", poItem.Amount.ToString(CultureInfo.InvariantCulture), invoiceItem.LineTotal.ToString(CultureInfo.InvariantCulture)));
        }

        foreach (var poItem in purchaseOrder.Items)
        {
            var poSku = poItem.Product?.SkuSupplier ?? string.Empty;
            if (!invoice.Items.Any(item => string.Equals(item.SupplierSku, poSku, StringComparison.OrdinalIgnoreCase)))
                invoice.Discrepancies.Add(CreateDiscrepancy(invoice.Id, null, DiscrepancyType.MissingFromInvoice, "SupplierSku", poSku, "N/A"));
        }

        if (Math.Abs(invoice.TotalAmount - purchaseOrder.TotalAmount) > AmountTolerance)
        {
            invoice.Discrepancies.Add(CreateDiscrepancy(
                invoice.Id,
                null,
                DiscrepancyType.AmountMismatch,
                "TotalAmount",
                purchaseOrder.TotalAmount.ToString(CultureInfo.InvariantCulture),
                invoice.TotalAmount.ToString(CultureInfo.InvariantCulture)));
        }

        await TransitionAsync(invoice, InvoiceStatus.Completed, "StatusChange", $"Invoice reconciliation completed with {invoice.Discrepancies.Count} discrepancies.");
        _logger.LogInformation("Invoice {InvoiceId} reconciled with {DiscrepancyCount} discrepancies", invoice.Id, invoice.Discrepancies.Count);
    }

    private async Task TransitionAsync(Invoice invoice, InvoiceStatus toStatus, string eventType, string message)
    {
        var fromStatus = invoice.Status;
        invoice.Status = toStatus;
        invoice.ProcessingLogs.Add(new InvoiceProcessingLog
        {
            FromStatus = fromStatus,
            ToStatus = toStatus,
            EventType = eventType,
            Message = message
        });
        await _invoiceRepository.UpdateAsync(invoice);
        await _unitOfWork.SaveChangesAsync();
    }

    private static Discrepancy CreateDiscrepancy(int invoiceId, int? invoiceItemId, DiscrepancyType type, string fieldName, string expectedValue, string actualValue) =>
        new()
        {
            InvoiceId = invoiceId,
            InvoiceItemId = invoiceItemId,
            DiscrepancyType = type,
            FieldName = fieldName,
            ExpectedValue = expectedValue,
            ActualValue = actualValue
        };
}
