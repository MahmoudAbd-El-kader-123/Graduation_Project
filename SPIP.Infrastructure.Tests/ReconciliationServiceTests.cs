using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SPIP.Application.DTOs.Invoice;
using SPIP.Application.Mapping;
using SPIP.Domain.Entities;
using SPIP.Domain.Enums;
using SPIP.Infrastructure.Persistence.Context;
using SPIP.Infrastructure.Repositories;
using SPIP.Infrastructure.Services;

namespace SPIP.Infrastructure.Tests;

public sealed class ReconciliationServiceTests
{
    private static readonly IMapper Mapper = new MapperConfiguration(
        configuration => configuration.AddProfile<InvoiceMappingProfile>(),
        NullLoggerFactory.Instance).CreateMapper();

    [Theory]
    [InlineData("SKU-1", "SKU-1")]
    [InlineData("sku-1", "SKU-1")]
    [InlineData("  SKU-1  ", "SKU-1")]
    public async Task Unique_normalized_sku_persists_matched_row(string invoiceSku, string poSku)
    {
        await using var scenario = await ReconciliationScenario.CreateAsync(
            [PurchaseOrderLine.Create(poSku)],
            [InvoiceLine.Create(invoiceSku)]);

        var invoice = await scenario.ReconcileAndReloadAsync();

        var row = Assert.Single(invoice.ReconciliationItems);
        Assert.Equal(ReconciliationItemStatus.Matched, row.Status);
        Assert.NotNull(row.PurchaseOrderItemId);
        Assert.NotNull(row.InvoiceItemId);
        Assert.Empty(row.Discrepancies);
    }

    [Theory]
    [InlineData(6, 11, 63.25, DiscrepancyType.QuantityMismatch)]
    [InlineData(5, 12, 63.25, DiscrepancyType.UnitPriceMismatch)]
    [InlineData(5, 11, 64.00, DiscrepancyType.AmountMismatch)]
    public async Task Changed_item_value_persists_different_row(
        int invoiceQuantity,
        double invoiceUnitPrice,
        double invoiceAmount,
        DiscrepancyType expectedType)
    {
        await using var scenario = await ReconciliationScenario.CreateAsync(
            [PurchaseOrderLine.Create("SKU-1")],
            [InvoiceLine.Create("SKU-1", invoiceQuantity, (decimal)invoiceUnitPrice, (decimal)invoiceAmount)]);

        var invoice = await scenario.ReconcileAndReloadAsync();

        var row = Assert.Single(invoice.ReconciliationItems);
        var discrepancy = Assert.Single(row.Discrepancies);
        Assert.Equal(ReconciliationItemStatus.Different, row.Status);
        Assert.Equal(expectedType, discrepancy.DiscrepancyType);
        Assert.Equal(row.PurchaseOrderItemId, discrepancy.PurchaseOrderItemId);
        Assert.Equal(row.InvoiceItemId, discrepancy.InvoiceItemId);
        Assert.Equal(row.Id, discrepancy.ReconciliationItemId);

        var response = Mapper.Map<InvoiceReconciliationDto>(invoice);
        Assert.Single(response.Discrepancies);
        Assert.Single(Assert.Single(response.Items).Discrepancies);
    }

    [Fact]
    public async Task Items_missing_from_opposite_document_persist_two_unmatched_rows()
    {
        await using var scenario = await ReconciliationScenario.CreateAsync(
            [PurchaseOrderLine.Create("PO-ONLY")],
            [InvoiceLine.Create("INVOICE-ONLY")]);

        var invoice = await scenario.ReconcileAndReloadAsync();

        var missingFromInvoice = Assert.Single(invoice.ReconciliationItems, row => row.Status == ReconciliationItemStatus.MissingFromInvoice);
        Assert.NotNull(missingFromInvoice.PurchaseOrderItemId);
        Assert.Null(missingFromInvoice.InvoiceItemId);

        var missingFromPo = Assert.Single(invoice.ReconciliationItems, row => row.Status == ReconciliationItemStatus.MissingFromPurchaseOrder);
        Assert.Null(missingFromPo.PurchaseOrderItemId);
        Assert.NotNull(missingFromPo.InvoiceItemId);

        var response = Mapper.Map<InvoiceReconciliationDto>(invoice);
        var missingFromInvoiceDto = Assert.Single(response.Items, row => row.Status == "MissingFromInvoice");
        Assert.NotNull(missingFromInvoiceDto.PurchaseOrderItemId);
        Assert.Null(missingFromInvoiceDto.InvoiceItemId);
        var missingFromPoDto = Assert.Single(response.Items, row => row.Status == "MissingFromPurchaseOrder");
        Assert.Null(missingFromPoDto.PurchaseOrderItemId);
        Assert.NotNull(missingFromPoDto.InvoiceItemId);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task Duplicate_or_repeated_sku_moves_invoice_to_needs_review_without_reusing_an_item(
        bool duplicateInvoiceSku,
        bool duplicatePoSku)
    {
        var poLines = duplicatePoSku
            ? new[] { PurchaseOrderLine.Create("SKU-1"), PurchaseOrderLine.Create(" sku-1 ") }
            : new[] { PurchaseOrderLine.Create("SKU-1") };
        var invoiceLines = duplicateInvoiceSku
            ? new[] { InvoiceLine.Create("SKU-1"), InvoiceLine.Create("sku-1") }
            : new[] { InvoiceLine.Create("SKU-1") };
        await using var scenario = await ReconciliationScenario.CreateAsync(poLines, invoiceLines);

        var invoice = await scenario.ReconcileAndReloadAsync();

        Assert.Equal(InvoiceStatus.NeedsReview, invoice.Status);
        Assert.Empty(invoice.ReconciliationItems);
        Assert.Empty(invoice.Discrepancies);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Missing_supplier_sku_moves_invoice_to_needs_review(bool missingFromInvoice)
    {
        var poSku = missingFromInvoice ? "SKU-1" : " ";
        var invoiceSku = missingFromInvoice ? " " : "SKU-1";
        await using var scenario = await ReconciliationScenario.CreateAsync(
            [PurchaseOrderLine.Create(poSku)],
            [InvoiceLine.Create(invoiceSku)]);

        var invoice = await scenario.ReconcileAndReloadAsync();

        Assert.Equal(InvoiceStatus.NeedsReview, invoice.Status);
        Assert.Empty(invoice.ReconciliationItems);
    }

    [Fact]
    public async Task Total_difference_remains_top_level_without_item_identifiers()
    {
        await using var scenario = await ReconciliationScenario.CreateAsync(
            [PurchaseOrderLine.Create("SKU-1")],
            [InvoiceLine.Create("SKU-1")],
            invoiceTotal: 70m);

        var invoice = await scenario.ReconcileAndReloadAsync();

        var discrepancy = Assert.Single(invoice.Discrepancies, difference => difference.FieldName == "TotalAmount");
        Assert.Null(discrepancy.PurchaseOrderItemId);
        Assert.Null(discrepancy.InvoiceItemId);
        Assert.Null(discrepancy.ReconciliationItemId);
    }

    [Fact]
    public async Task Snapshot_values_do_not_change_when_purchase_order_item_changes()
    {
        await using var scenario = await ReconciliationScenario.CreateAsync(
            [PurchaseOrderLine.Create("SKU-1")],
            [InvoiceLine.Create("SKU-1")]);
        var reconciledInvoice = await scenario.ReconcileAndReloadAsync();
        var expectedQuantity = Assert.Single(reconciledInvoice.ReconciliationItems).ExpectedQuantity;

        await scenario.ChangePurchaseOrderQuantityAsync(99);
        var reloadedInvoice = await scenario.ReloadAsync();
        var response = Mapper.Map<InvoiceReconciliationDto>(reloadedInvoice);

        Assert.Equal(expectedQuantity, Assert.Single(reloadedInvoice.ReconciliationItems).ExpectedQuantity);
        Assert.Single(response.Items);
        Assert.Empty(response.Discrepancies);
        Assert.Empty(Assert.Single(response.Items).Discrepancies);
    }
}

internal sealed record PurchaseOrderLine(string Sku, int Quantity, decimal UnitPrice, decimal Amount)
{
    public static PurchaseOrderLine Create(string sku, int quantity = 5, decimal unitPrice = 11m, decimal amount = 63.25m) =>
        new(sku, quantity, unitPrice, amount);
}

internal sealed record InvoiceLine(string Sku, int Quantity, decimal UnitPrice, decimal Amount)
{
    public static InvoiceLine Create(string sku, int quantity = 5, decimal unitPrice = 11m, decimal amount = 63.25m) =>
        new(sku, quantity, unitPrice, amount);
}

internal sealed class ReconciliationScenario : IAsyncDisposable
{
    private readonly string _databaseName;
    private readonly DbContextOptions<ApplicationDbContext> _options;
    private readonly int _invoiceId;

    private ReconciliationScenario(string databaseName, DbContextOptions<ApplicationDbContext> options, int invoiceId)
    {
        _databaseName = databaseName;
        _options = options;
        _invoiceId = invoiceId;
    }

    public static async Task<ReconciliationScenario> CreateAsync(
        IReadOnlyCollection<PurchaseOrderLine> poLines,
        IReadOnlyCollection<InvoiceLine> invoiceLines,
        decimal invoiceTotal = 63.25m)
    {
        var databaseName = $"SPIP_Reconciliation_Test_{Guid.NewGuid():N}";
        var connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connectionString).Options;
        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var invoiceId = await SeedAsync(context, poLines, invoiceLines, invoiceTotal);
        return new ReconciliationScenario(databaseName, options, invoiceId);
    }

    public async Task<Invoice> ReconcileAndReloadAsync()
    {
        await using (var context = new ApplicationDbContext(_options))
        {
            var invoiceRepository = new InvoiceRepository(context);
            var service = new ReconciliationService(
                invoiceRepository,
                new PurchaseOrderRepository(context),
                new UnitOfWork(context),
                NullLogger<ReconciliationService>.Instance);
            await service.ReconcileAsync(_invoiceId);
        }

        return await ReloadAsync();
    }

    public async Task<Invoice> ReloadAsync()
    {
        await using var context = new ApplicationDbContext(_options);
        return await new InvoiceRepository(context).GetWithDetailsByIdAsync(_invoiceId)
            ?? throw new InvalidOperationException("Test invoice was not found.");
    }

    public async Task ChangePurchaseOrderQuantityAsync(int quantity)
    {
        await using var context = new ApplicationDbContext(_options);
        var poItem = await context.PurchaseOrderItems.SingleAsync();
        poItem.Quantity = quantity;
        await context.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await using var context = new ApplicationDbContext(_options);
        await context.Database.EnsureDeletedAsync();
    }

    private static async Task<int> SeedAsync(
        ApplicationDbContext context,
        IEnumerable<PurchaseOrderLine> poLines,
        IEnumerable<InvoiceLine> invoiceLines,
        decimal invoiceTotal)
    {
        var user = new User { IdentityId = Guid.NewGuid(), FullName = "Test User", Email = "test@example.com", RoleId = Guid.NewGuid(), RoleName = "Admin" };
        var vendor = new Vendor { Name = "Test Vendor", IsApproved = true };
        var purchaseOrder = new PurchaseOrder { Vendor = vendor, RequestedByUser = user, TotalAmount = 63.25m };
        foreach (var line in poLines)
        {
            purchaseOrder.Items.Add(new PurchaseOrderItem
            {
                Product = new Product { Vendor = vendor, Name = line.Sku, SkuSupplier = line.Sku },
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                Amount = line.Amount
            });
        }

        var invoice = new Invoice { Vendor = vendor, PurchaseOrder = purchaseOrder, UploadedByUser = user, TotalAmount = invoiceTotal, Status = InvoiceStatus.Validated };
        foreach (var line in invoiceLines)
            invoice.Items.Add(new InvoiceItem { SupplierSku = line.Sku, Quantity = line.Quantity, UnitPrice = line.UnitPrice, LineTotal = line.Amount });

        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();
        return invoice.Id;
    }
}
