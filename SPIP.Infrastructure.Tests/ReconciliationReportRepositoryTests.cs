using Microsoft.EntityFrameworkCore;
using SPIP.Application.DTOs.ReconciliationReport;
using SPIP.Domain.Entities;
using SPIP.Domain.Enums;
using SPIP.Infrastructure.Persistence.Context;
using SPIP.Infrastructure.Repositories;

namespace SPIP.Infrastructure.Tests;

public sealed class ReconciliationReportRepositoryTests
{
    [Fact]
    public async Task Summary_projection_returns_exact_nonduplicated_counts()
    {
        await using var database = await ReportDatabase.CreateAsync();
        var invoice = database.CreateInvoice("INV-COUNT", "PO-COUNT", "Vendor Alpha", "uploader@example.com");
        AddRow(invoice, ReconciliationItemStatus.Different, DiscrepancyType.QuantityMismatch);
        AddRow(invoice, ReconciliationItemStatus.MissingFromInvoice, DiscrepancyType.MissingFromInvoice);
        AddRow(invoice, ReconciliationItemStatus.MissingFromPurchaseOrder, DiscrepancyType.MissingSku);
        AddRow(invoice, ReconciliationItemStatus.Matched);
        invoice.Discrepancies.Add(new Discrepancy { DiscrepancyType = DiscrepancyType.AmountMismatch, FieldName = "TotalAmount" });
        await database.SaveAsync(invoice);

        var (reports, totalCount) = await database.Repository.GetPageAsync(new ReconciliationReportParameters());

        var report = Assert.Single(reports);
        Assert.Equal(1, totalCount);
        Assert.Equal(4, report.DiscrepancyCount);
        Assert.True(report.HasDiscrepancies);
        Assert.Equal(1, report.DifferentItemCount);
        Assert.Equal(1, report.MissingFromInvoiceCount);
        Assert.Equal(1, report.MissingFromPurchaseOrderCount);
    }

    [Theory]
    [InlineData("INV-SEARCH")]
    [InlineData("PO-SEARCH")]
    [InlineData("Vendor Search")]
    [InlineData("searcher@example.com")]
    public async Task Search_matches_each_supported_persisted_field(string searchTerm)
    {
        await using var database = await ReportDatabase.CreateAsync();
        await database.SaveAsync(database.CreateInvoice("INV-SEARCH", "PO-SEARCH", "Vendor Search", "searcher@example.com"));
        await database.SaveAsync(database.CreateInvoice("OTHER", "OTHER-PO", "Other Vendor", "other@example.com"));

        var (reports, totalCount) = await database.Repository.GetPageAsync(new ReconciliationReportParameters { SearchTerm = searchTerm });

        Assert.Equal(1, totalCount);
        Assert.Equal("INV-SEARCH", Assert.Single(reports).InvoiceNumber);
    }

    [Fact]
    public async Task Filters_sort_and_pagination_are_applied_in_stable_order()
    {
        await using var database = await ReportDatabase.CreateAsync();
        var first = database.CreateInvoice("FIRST", "PO-1", "Vendor", "u@example.com", new DateTime(2026, 8, 10), InvoiceStatus.Completed);
        AddRow(first, ReconciliationItemStatus.Different, DiscrepancyType.QuantityMismatch);
        var second = database.CreateInvoice("SECOND", "PO-2", "Vendor", "u@example.com", new DateTime(2026, 8, 11), InvoiceStatus.Completed);
        AddRow(second, ReconciliationItemStatus.Different, DiscrepancyType.QuantityMismatch);
        AddRow(second, ReconciliationItemStatus.Different, DiscrepancyType.AmountMismatch);
        await database.SaveAsync(first);
        await database.SaveAsync(second);
        await database.SaveAsync(database.CreateInvoice("FAILED", "PO-3", "Vendor", "u@example.com", new DateTime(2026, 8, 12), InvoiceStatus.Failed));

        var parameters = new ReconciliationReportParameters
        {
            PageSize = 1,
            PageNumber = 1,
            Status = "Completed",
            HasDiscrepancies = true,
            FromDate = new DateTime(2026, 8, 9),
            ToDate = new DateTime(2026, 8, 12),
            SortBy = "discrepancyCount",
            SortDirection = "desc"
        };

        var (reports, totalCount) = await database.Repository.GetPageAsync(parameters);

        Assert.Equal(2, totalCount);
        Assert.Equal("SECOND", Assert.Single(reports).InvoiceNumber);
    }

    [Fact]
    public async Task Empty_query_returns_empty_page()
    {
        await using var database = await ReportDatabase.CreateAsync();

        var (reports, totalCount) = await database.Repository.GetPageAsync(new ReconciliationReportParameters());

        Assert.Empty(reports);
        Assert.Equal(0, totalCount);
    }

    private static void AddRow(
        Invoice invoice,
        ReconciliationItemStatus status,
        DiscrepancyType? discrepancyType = null)
    {
        var row = new InvoiceReconciliationItem { Status = status };
        invoice.ReconciliationItems.Add(row);
        if (!discrepancyType.HasValue)
            return;

        var discrepancy = new Discrepancy
        {
            DiscrepancyType = discrepancyType.Value,
            FieldName = discrepancyType.Value.ToString(),
            ReconciliationItem = row
        };
        row.Discrepancies.Add(discrepancy);
        invoice.Discrepancies.Add(discrepancy);
    }
}

internal sealed class ReportDatabase : IAsyncDisposable
{
    private readonly string _databaseName;
    private readonly ApplicationDbContext _context;

    private ReportDatabase(string databaseName, ApplicationDbContext context)
    {
        _databaseName = databaseName;
        _context = context;
        Repository = new ReconciliationReportRepository(context);
    }

    public ReconciliationReportRepository Repository { get; }

    public static async Task<ReportDatabase> CreateAsync()
    {
        var databaseName = $"SPIP_Report_Test_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer($"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return new ReportDatabase(databaseName, context);
    }

    public Invoice CreateInvoice(
        string invoiceNumber,
        string purchaseOrderNumber,
        string vendorName,
        string uploaderEmail,
        DateTime? uploadedAt = null,
        InvoiceStatus status = InvoiceStatus.Completed)
    {
        var user = new User
        {
            IdentityId = Guid.NewGuid(),
            FullName = uploaderEmail,
            Email = uploaderEmail,
            RoleId = Guid.NewGuid(),
            RoleName = "Accountant"
        };
        var vendor = new Vendor { Name = vendorName, IsApproved = true };
        return new Invoice
        {
            InvoiceNumber = invoiceNumber,
            VendorName = vendorName,
            Vendor = vendor,
            UploadedByUser = user,
            PurchaseOrder = new PurchaseOrder
            {
                OrderNumber = purchaseOrderNumber,
                Vendor = vendor,
                RequestedByUser = user
            },
            InvoiceDate = new DateTime(2026, 8, 12),
            Status = status,
            CreatedAt = uploadedAt ?? DateTime.UtcNow
        };
    }

    public async Task SaveAsync(Invoice invoice)
    {
        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }
}
