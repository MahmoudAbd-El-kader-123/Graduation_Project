namespace SPIP.Application.DTOs.Dashboard;

public class DashboardStatsDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int InactiveUsers { get; set; }
    public Dictionary<string, int> UsersPerRole { get; set; } = new();
    
    public int TotalRoles { get; set; }
    public int TotalPermissions { get; set; }
}
