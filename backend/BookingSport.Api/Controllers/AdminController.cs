using BookingSport.Api.DTOs.Auth;
using BookingSport.Api.DTOs.Bookings;
using BookingSport.Api.Services.Auth;
using BookingSport.Api.Services.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingSport.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class AdminController(
    IAuthSettingsService authSettingsService,
    IAdminBookingReportService adminBookingReportService,
    ILogger<AdminController> logger) : ControllerBase
{
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new { message = "admin ok" });
    }

    [HttpGet("auth-settings")]
    public async Task<ActionResult<AuthSettingsResponse>> GetAuthSettings(CancellationToken cancellationToken)
    {
        var settings = await authSettingsService.GetGlobalSettingsAsync(cancellationToken);

        return Ok(settings);
    }

    [HttpPut("auth-settings")]
    public async Task<ActionResult<AuthSettingsResponse>> UpdateAuthSettings(
        AuthSettingsUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authSettingsService.UpdateGlobalSettingsAsync(request, cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new { message = result.Error });
        }

        logger.LogInformation(
            "Auth lifetime settings were updated. AccessTokenMinutes={AccessTokenMinutes}, RefreshTokenDays={RefreshTokenDays}",
            result.AuthSettings!.AccessTokenMinutes,
            result.AuthSettings.RefreshTokenDays);

        return Ok(result.AuthSettings);
    }

    [HttpGet("reports/bookings/export")]
    public async Task<IActionResult> ExportBookings(
        [FromQuery] BookingQueryParameters query,
        CancellationToken cancellationToken)
    {
        var report = await adminBookingReportService.ExportBookingsAsync(query, cancellationToken);

        logger.LogInformation(
            "Admin exported booking report. FromDate={FromDate}, ToDate={ToDate}, CourtId={CourtId}, Status={Status}",
            query.FromDate,
            query.ToDate,
            query.CourtId,
            query.Status);

        return File(report.Content, report.ContentType, report.FileName);
    }
}
