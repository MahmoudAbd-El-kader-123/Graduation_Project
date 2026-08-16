namespace SPIP.Application.DTOs.Dashboard;

public class DashboardSummaryDto
{
    public int TotalPurchaseOrders { get; set; }
    public int PendingApprovals { get; set; }
    public int TotalVendors { get; set; }
    public decimal TotalSpend { get; set; }
}
