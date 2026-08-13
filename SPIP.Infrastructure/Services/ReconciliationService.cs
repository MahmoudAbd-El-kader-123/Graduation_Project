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

        var validationError = FindMatchingValidationError(invoice, purchaseOrder);
        if (validationError is not null)
        {
            await TransitionAsync(invoice, InvoiceStatus.NeedsReview, "ValidationError", validationError);
            return;
        }

        await TransitionAsync(invoice, InvoiceStatus.Compared, "StatusChange", "Invoice comparison started.");

        _invoiceRepository.ClearReconciliationResults(invoice);
        BuildReconciliationRows(invoice, purchaseOrder);
        AddTotalDiscrepancy(invoice, purchaseOrder);

        await TransitionAsync(invoice, InvoiceStatus.Completed, "StatusChange", $"Invoice reconciliation completed with {invoice.Discrepancies.Count} discrepancies.");
        _logger.LogInformation("Invoice {InvoiceId} reconciled with {DiscrepancyCount} discrepancies", invoice.Id, invoice.Discrepancies.Count);
    }

    private static string? FindMatchingValidationError(Invoice invoice, PurchaseOrder purchaseOrder)
    {
        if (invoice.Items.Any(item => string.IsNullOrWhiteSpace(item.SupplierSku)))
            return "Invoice contains an item without a supplier SKU.";

        if (purchaseOrder.Items.Any(item => string.IsNullOrWhiteSpace(item.Product?.SkuSupplier)))
            return "Purchase order contains an item without a supplier SKU.";

        if (HasDuplicateSku(invoice.Items.Select(item => item.SupplierSku)))
            return "Invoice contains duplicate supplier SKUs and requires manual review.";

        return HasDuplicateSku(purchaseOrder.Items.Select(item => item.Product!.SkuSupplier!))
            ? "Purchase order contains duplicate supplier SKUs and requires manual review."
            : null;
    }

    private static bool HasDuplicateSku(IEnumerable<string> skus) =>
        skus.GroupBy(NormalizeSku, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1);

    private static string NormalizeSku(string sku) => sku.Trim();

    private static void BuildReconciliationRows(Invoice invoice, PurchaseOrder purchaseOrder)
    {
        var poItemsBySku = purchaseOrder.Items.ToDictionary(
            item => NormalizeSku(item.Product!.SkuSupplier!),
            StringComparer.OrdinalIgnoreCase);
        var matchedPurchaseOrderItemIds = new HashSet<int>();

        foreach (var invoiceItem in invoice.Items)
        {
            var normalizedSku = NormalizeSku(invoiceItem.SupplierSku);
            if (!poItemsBySku.TryGetValue(normalizedSku, out var purchaseOrderItem))
            {
                AddMissingFromPurchaseOrderRow(invoice, invoiceItem);
                continue;
            }

            matchedPurchaseOrderItemIds.Add(purchaseOrderItem.Id);
            AddMatchedRow(invoice, purchaseOrderItem, invoiceItem);
        }

        foreach (var purchaseOrderItem in purchaseOrder.Items.Where(item => !matchedPurchaseOrderItemIds.Contains(item.Id)))
            AddMissingFromInvoiceRow(invoice, purchaseOrderItem);
    }

    private static void AddMatchedRow(Invoice invoice, PurchaseOrderItem poItem, InvoiceItem invoiceItem)
    {
        var row = CreateMatchedRow(invoice, poItem, invoiceItem);
        AddQuantityDifference(invoice, row, poItem.Quantity, invoiceItem.Quantity);
        AddUnitPriceDifference(invoice, row, poItem.UnitPrice, invoiceItem.UnitPrice);
        AddAmountDifference(invoice, row, poItem.Amount, invoiceItem.LineTotal);
        row.Status = row.Discrepancies.Count == 0 ? ReconciliationItemStatus.Matched : ReconciliationItemStatus.Different;
        invoice.ReconciliationItems.Add(row);
    }

    private static InvoiceReconciliationItem CreateMatchedRow(Invoice invoice, PurchaseOrderItem poItem, InvoiceItem invoiceItem) =>
        new()
        {
            InvoiceId = invoice.Id,
            PurchaseOrderItemId = poItem.Id,
            InvoiceItemId = invoiceItem.Id,
            PurchaseOrderSku = poItem.Product!.SkuSupplier,
            InvoiceSku = invoiceItem.SupplierSku,
            ProductName = poItem.Product.Name,
            ExpectedQuantity = poItem.Quantity,
            ActualQuantity = invoiceItem.Quantity,
            ExpectedUnitPrice = poItem.UnitPrice,
            ActualUnitPrice = invoiceItem.UnitPrice,
            ExpectedAmount = poItem.Amount,
            ActualAmount = invoiceItem.LineTotal
        };

    private static void AddMissingFromPurchaseOrderRow(Invoice invoice, InvoiceItem invoiceItem)
    {
        var row = new InvoiceReconciliationItem
        {
            InvoiceId = invoice.Id,
            InvoiceItemId = invoiceItem.Id,
            InvoiceSku = invoiceItem.SupplierSku,
            ProductName = invoiceItem.Description,
            ActualQuantity = invoiceItem.Quantity,
            ActualUnitPrice = invoiceItem.UnitPrice,
            ActualAmount = invoiceItem.LineTotal,
            Status = ReconciliationItemStatus.MissingFromPurchaseOrder
        };
        AddDiscrepancy(invoice, row, new Difference(DiscrepancyType.MissingSku, "SupplierSku", "N/A", invoiceItem.SupplierSku));
        invoice.ReconciliationItems.Add(row);
    }

    private static void AddMissingFromInvoiceRow(Invoice invoice, PurchaseOrderItem poItem)
    {
        var row = new InvoiceReconciliationItem
        {
            InvoiceId = invoice.Id,
            PurchaseOrderItemId = poItem.Id,
            PurchaseOrderSku = poItem.Product!.SkuSupplier,
            ProductName = poItem.Product.Name,
            ExpectedQuantity = poItem.Quantity,
            ExpectedUnitPrice = poItem.UnitPrice,
            ExpectedAmount = poItem.Amount,
            Status = ReconciliationItemStatus.MissingFromInvoice
        };
        AddDiscrepancy(invoice, row, new Difference(DiscrepancyType.MissingFromInvoice, "SupplierSku", row.PurchaseOrderSku!, "N/A"));
        invoice.ReconciliationItems.Add(row);
    }

    private static void AddQuantityDifference(Invoice invoice, InvoiceReconciliationItem row, int expected, int actual)
    {
        if (expected != actual)
            AddDiscrepancy(invoice, row, new Difference(DiscrepancyType.QuantityMismatch, "Quantity", FormatValue(expected), FormatValue(actual)));
    }

    private static void AddUnitPriceDifference(Invoice invoice, InvoiceReconciliationItem row, decimal expected, decimal actual)
    {
        if (expected != actual)
            AddDiscrepancy(invoice, row, new Difference(DiscrepancyType.UnitPriceMismatch, "UnitPrice", FormatValue(expected), FormatValue(actual)));
    }

    private static void AddAmountDifference(Invoice invoice, InvoiceReconciliationItem row, decimal expected, decimal actual)
    {
        if (Math.Abs(expected - actual) > AmountTolerance)
            AddDiscrepancy(invoice, row, new Difference(DiscrepancyType.AmountMismatch, "Amount", FormatValue(expected), FormatValue(actual)));
    }

    private static string FormatValue<T>(T value) => Convert.ToString(value, CultureInfo.InvariantCulture)!;

    private static void AddDiscrepancy(Invoice invoice, InvoiceReconciliationItem row, Difference difference)
    {
        var discrepancy = CreateDiscrepancy(invoice.Id, row, difference);
        row.Discrepancies.Add(discrepancy);
        invoice.Discrepancies.Add(discrepancy);
    }

    private static void AddTotalDiscrepancy(Invoice invoice, PurchaseOrder purchaseOrder)
    {
        if (Math.Abs(invoice.TotalAmount - purchaseOrder.TotalAmount) <= AmountTolerance)
            return;

        invoice.Discrepancies.Add(new Discrepancy
        {
            InvoiceId = invoice.Id,
            DiscrepancyType = DiscrepancyType.AmountMismatch,
            FieldName = "TotalAmount",
            ExpectedValue = purchaseOrder.TotalAmount.ToString(CultureInfo.InvariantCulture),
            ActualValue = invoice.TotalAmount.ToString(CultureInfo.InvariantCulture)
        });
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

    private static Discrepancy CreateDiscrepancy(int invoiceId, InvoiceReconciliationItem row, Difference difference) =>
        new()
        {
            InvoiceId = invoiceId,
            PurchaseOrderItemId = row.PurchaseOrderItemId,
            InvoiceItemId = row.InvoiceItemId,
            ReconciliationItem = row,
            DiscrepancyType = difference.Type,
            FieldName = difference.FieldName,
            ExpectedValue = difference.Expected,
            ActualValue = difference.Actual
        };

    private sealed record Difference(
        DiscrepancyType Type,
        string FieldName,
        string Expected,
        string Actual);
}
