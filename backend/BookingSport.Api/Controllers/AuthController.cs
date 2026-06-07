using System.Security.Claims;
using BookingSport.Api.DTOs.Auth;
using BookingSport.Api.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingSport.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    IAuthService authService,
    IAuthSettingsService authSettingsService,
    IConfiguration configuration) : ControllerBase
{
    private string RefreshCookieName => configuration["Auth:RefreshCookieName"] ?? "bookingSport.refresh";

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.RegisterAsync(request, cancellationToken);

        if (!result.Succeeded)
        {
            return Conflict(new { message = result.Error });
        }

        SetRefreshCookie(result);

        return Ok(result.Response);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);

        if (!result.Succeeded)
        {
            return Unauthorized(new { message = result.Error });
        }

        SetRefreshCookie(result);

        return Ok(result.Response);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[RefreshCookieName];
        var result = await authService.RefreshAsync(
            refreshToken ?? string.Empty,
            Request.Headers.UserAgent.ToString(),
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken);

        if (!result.Succeeded)
        {
            DeleteRefreshCookie();
            return Unauthorized(new { message = result.Error });
        }

        SetRefreshCookie(result);

        return Ok(result.Response);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(Request.Cookies[RefreshCookieName], cancellationToken);
        DeleteRefreshCookie();

        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserResponse>> Me(CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        var user = await authService.GetCurrentUserAsync(userId, cancellationToken);

        return user is null ? Unauthorized() : Ok(user);
    }

    [Authorize]
    [HttpGet("session-settings")]
    public async Task<ActionResult<UserSessionSettingsResponse>> GetSessionSettings(CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        if (userId is null)
        {
            return Unauthorized();
        }

        var settings = await authSettingsService.GetUserSessionSettingsAsync(userId.Value, cancellationToken);

        return Ok(settings);
    }

    [Authorize]
    [HttpPut("session-settings")]
    public async Task<ActionResult<UserSessionSettingsResponse>> UpdateSessionSettings(
        UserSessionSettingsUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await authSettingsService.UpdateUserSessionSettingsAsync(
            userId.Value,
            request,
            cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(result.UserSessionSettings);
    }

    private Guid? GetUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(userIdValue, out var userId) ? userId : null;
    }

    private void SetRefreshCookie(AuthResult result)
    {
        if (string.IsNullOrWhiteSpace(result.RefreshToken) || result.RefreshTokenExpiresAt is null)
        {
            return;
        }

        Response.Cookies.Append(
            RefreshCookieName,
            result.RefreshToken,
            CreateRefreshCookieOptions(result.RefreshTokenExpiresAt.Value));
    }

    private void DeleteRefreshCookie()
    {
        Response.Cookies.Delete(RefreshCookieName, CreateRefreshCookieOptions(DateTimeOffset.UtcNow));
    }

    private CookieOptions CreateRefreshCookieOptions(DateTimeOffset expiresAt)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = ReadBool("Auth:CookieSecure", true),
            SameSite = ReadSameSiteMode(),
            Expires = expiresAt,
            MaxAge = expiresAt - DateTimeOffset.UtcNow,
            Path = "/api/auth"
        };
    }

    private bool ReadBool(string key, bool fallback)
    {
        return bool.TryParse(configuration[key], out var value) ? value : fallback;
    }

    private SameSiteMode ReadSameSiteMode()
    {
        return Enum.TryParse<SameSiteMode>(
            configuration["Auth:CookieSameSite"],
            ignoreCase: true,
            out var sameSiteMode)
            ? sameSiteMode
            : SameSiteMode.Lax;
    }
}
