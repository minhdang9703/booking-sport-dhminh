using BookingSport.Api.DTOs.Dashboard;

namespace BookingSport.Api.Services.Dashboard;

public interface IDashboardService
{
    Task<DashboardResult<RevenueDashboardResponse>> GetRevenueDashboardAsync(
        RevenueDashboardQuery query,
        CancellationToken cancellationToken);
}
