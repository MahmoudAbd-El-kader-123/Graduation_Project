namespace SPIP.Application.DTOs.ReconciliationReport;

public sealed class ReconciliationReportParameters
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public string? SearchTerm { get; set; }
    public string? Status { get; set; }
    public bool? HasDiscrepancies { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string SortBy { get; set; } = "uploadedAt";
    public string SortDirection { get; set; } = "desc";
}
