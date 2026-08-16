using SPIP.Application.DTOs.Dashboard;
using SPIP.Shared.Result;

namespace SPIP.Application.Interfaces.Services;

public interface IDashboardService
{
    Task<Result<DashboardStatsDto>> GetStatsAsync();
}
