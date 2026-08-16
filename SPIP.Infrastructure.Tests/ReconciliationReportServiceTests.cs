using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging.Abstractions;
using SPIP.API.Controllers;
using SPIP.Application.DTOs.ReconciliationReport;
using SPIP.Application.Interfaces.Repositories;
using SPIP.Application.Mapping;
using SPIP.Domain.Constants;
using SPIP.Domain.Entities;
using SPIP.Domain.Enums;
using SPIP.Infrastructure.Services;

namespace SPIP.Infrastructure.Tests;

public sealed class ReconciliationReportServiceTests
{
    private static readonly IMapper Mapper = new MapperConfiguration(
        configuration => configuration.AddProfile<InvoiceMappingProfile>(),
        NullLoggerFactory.Instance).CreateMapper();

    [Theory]
    [MemberData(nameof(InvalidQueries))]
    public async Task Invalid_report_query_is_rejected(ReconciliationReportParameters parameters, string expectedError)
    {
        var service = new ReconciliationReportService(new ReportRepositoryStub(), Mapper);

        var result = await service.GetPageAsync(parameters);

        Assert.False(result.Succeeded);
        Assert.Contains(expectedError, result.Errors!);
    }

    [Fact]
    public async Task Failed_report_hides_persisted_technical_error()
    {
        var invoice = CreateInvoice(InvoiceStatus.Failed);
        invoice.ProcessingLogs.Add(new InvoiceProcessingLog
        {
            EventType = "ProcessingError",
            Message = "Server=secret;Database=SPIP_DB; SQL connection failed"
        });
        var service = new ReconciliationReportService(new ReportRepositoryStub(invoice), Mapper);

        var result = await service.GetDetailAsync(invoice.Id);

        Assert.True(result.Succeeded);
        Assert.Equal("Invoice processing failed. Contact support if the problem continues.", result.Data!.StatusMessage);
        Assert.DoesNotContain("Server=", result.Data.StatusMessage);
    }

    [Fact]
    public async Task Manager_detail_returns_complete_persisted_rows_without_mutating_invoice()
    {
        var invoice = CreateInvoice(InvoiceStatus.Completed);
        foreach (var status in Enum.GetValues<ReconciliationItemStatus>())
            invoice.ReconciliationItems.Add(new InvoiceReconciliationItem { Status = status });
        var originalStatus = invoice.Status;
        var service = new ReconciliationReportService(new ReportRepositoryStub(invoice), Mapper);

        var result = await service.GetDetailAsync(invoice.Id);

        Assert.True(result.Succeeded);
        Assert.Equal(Enum.GetNames<ReconciliationItemStatus>(), result.Data!.Reconciliation.Items.Select(row => row.Status));
        Assert.Equal(originalStatus, invoice.Status);
        Assert.Equal(4, invoice.ReconciliationItems.Count);
    }

    [Fact]
    public void Reporting_controller_requires_dedicated_permission()
    {
        var authorization = Assert.Single(
            typeof(ReconciliationReportsController).GetCustomAttributes(typeof(AuthorizeAttribute), true)
                .Cast<AuthorizeAttribute>());

        Assert.Equal(Permissions.ReconciliationReports.ViewAll, authorization.Policy);
        Assert.NotEqual(Permissions.Invoices.ViewAll, authorization.Policy);
        Assert.NotEqual(Permissions.Reports.View, authorization.Policy);
    }

    [Fact]
    public void Existing_reconciliation_endpoint_keeps_invoice_view_permission()
    {
        var method = typeof(InvoicesController).GetMethod(nameof(InvoicesController.GetReconciliation));
        var authorization = Assert.Single(method!.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());

        Assert.Equal(Permissions.Invoices.View, authorization.Policy);
    }

    public static TheoryData<ReconciliationReportParameters, string> InvalidQueries => new()
    {
        { new() { PageNumber = 0 }, "PageNumber must be between 1 and 1000000." },
        { new() { PageNumber = 1_000_001 }, "PageNumber must be between 1 and 1000000." },
        { new() { PageSize = 51 }, "PageSize must be between 1 and 50." },
        { new() { SearchTerm = new string('x', 101) }, "SearchTerm must not exceed 100 characters." },
        { new() { Status = "NotAStatus" }, "Status is invalid." },
        { new() { Status = "1" }, "Status is invalid." },
        { new() { SortBy = "uploadedByUserId" }, "SortBy must be uploadedAt or discrepancyCount." },
        { new() { SortDirection = "sideways" }, "SortDirection must be asc or desc." },
        { new() { FromDate = new DateTime(2026, 2, 2), ToDate = new DateTime(2026, 2, 1) }, "FromDate must not be later than ToDate." },
        { new() { ToDate = DateTime.MaxValue }, "ToDate is outside the supported range." }
    };

    private static Invoice CreateInvoice(InvoiceStatus status) => new()
    {
        Id = 42,
        InvoiceNumber = "INV-42",
        VendorName = "Vendor",
        PurchaseOrderId = 7,
        Status = status,
        InvoiceDate = new DateTime(2026, 8, 12),
        UploadedByUser = new User { Email = "manager-report@example.com" },
        PurchaseOrder = new PurchaseOrder { OrderNumber = "PO-7" }
    };
}

internal sealed class ReportRepositoryStub : IReconciliationReportRepository
{
    private readonly Invoice? _invoice;

    public ReportRepositoryStub(Invoice? invoice = null)
    {
        _invoice = invoice;
    }

    public Task<(IReadOnlyList<ReconciliationReportSummaryDto> Items, int TotalCount)> GetPageAsync(
        ReconciliationReportParameters parameters,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(((IReadOnlyList<ReconciliationReportSummaryDto>)[], 0));

    public Task<Invoice?> GetDetailAsync(int invoiceId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_invoice?.Id == invoiceId ? _invoice : null);
}
