using BookingSport.Api.DTOs.Dashboard;
using BookingSport.Api.Services.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingSport.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/[controller]")]
public class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    [HttpGet("revenue")]
    public async Task<ActionResult<RevenueDashboardResponse>> GetRevenueDashboard(
        [FromQuery] RevenueDashboardQuery query,
        CancellationToken cancellationToken)
    {
        var result = await dashboardService.GetRevenueDashboardAsync(query, cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(result.Value);
    }
}
