namespace SPIP.Application.DTOs.Dashboard;

public class DashboardStatsDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int InactiveUsers { get; set; }
    public int AdminUsers { get; set; }
    public int ManagerUsers { get; set; }
    public int AccountantUsers { get; set; }
    
    public int TotalRoles { get; set; }
    public int TotalPermissions { get; set; }
}
