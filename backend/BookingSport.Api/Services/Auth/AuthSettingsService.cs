using BookingSport.Api.Data;
using BookingSport.Api.DTOs.Auth;
using BookingSport.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookingSport.Api.Services.Auth;

public class AuthSettingsService(AppDbContext dbContext, IConfiguration configuration) : IAuthSettingsService
{
    private const string AccessTokenMinutesKey = "Auth.AccessTokenMinutes";
    private const string RefreshTokenDaysKey = "Auth.RefreshTokenDays";
    private const int DefaultAccessTokenMinutes = 15;
    private const int DefaultRefreshTokenDays = 14;
    private const int MinAccessTokenMinutes = 5;
    private const int MaxAccessTokenMinutes = 60;
    private const int MinRefreshTokenDays = 1;
    private const int MaxRefreshTokenDays = 30;

    public async Task<AuthLifetimeSettings> GetEffectiveLifetimeAsync(
        Guid? userId,
        CancellationToken cancellationToken)
    {
        var global = await GetGlobalLifetimeAsync(cancellationToken);

        if (userId is null)
        {
            return global;
        }

        var userSettings = await dbContext.UserAuthSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(settings => settings.UserId == userId.Value, cancellationToken);

        if (userSettings is null)
        {
            return global;
        }

        return new AuthLifetimeSettings(
            userSettings.AccessTokenMinutes ?? global.AccessTokenMinutes,
            userSettings.RefreshTokenDays ?? global.RefreshTokenDays);
    }

    public async Task<AuthSettingsResponse> GetGlobalSettingsAsync(CancellationToken cancellationToken)
    {
        var lifetime = await GetGlobalLifetimeAsync(cancellationToken);

        return MapGlobalResponse(lifetime);
    }

    public async Task<AuthSettingsResult> UpdateGlobalSettingsAsync(
        AuthSettingsUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateLifetime(request.AccessTokenMinutes, request.RefreshTokenDays);

        if (validationError is not null)
        {
            return AuthSettingsResult.Failure(validationError);
        }

        await UpsertSettingAsync(
            AccessTokenMinutesKey,
            request.AccessTokenMinutes.ToString(),
            cancellationToken);
        await UpsertSettingAsync(
            RefreshTokenDaysKey,
            request.RefreshTokenDays.ToString(),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return AuthSettingsResult.Success(MapGlobalResponse(
            new AuthLifetimeSettings(request.AccessTokenMinutes, request.RefreshTokenDays)));
    }

    public async Task<UserSessionSettingsResponse> GetUserSessionSettingsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var global = await GetGlobalLifetimeAsync(cancellationToken);
        var userSettings = await dbContext.UserAuthSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(settings => settings.UserId == userId, cancellationToken);

        return MapUserResponse(
            userSettings?.AccessTokenMinutes ?? global.AccessTokenMinutes,
            userSettings?.RefreshTokenDays ?? global.RefreshTokenDays,
            global);
    }

    public async Task<AuthSettingsResult> UpdateUserSessionSettingsAsync(
        Guid userId,
        UserSessionSettingsUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateLifetime(request.AccessTokenMinutes, request.RefreshTokenDays);

        if (validationError is not null)
        {
            return AuthSettingsResult.Failure(validationError);
        }

        var global = await GetGlobalLifetimeAsync(cancellationToken);
        var settings = await dbContext.UserAuthSettings
            .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);

        if (settings is null)
        {
            settings = new UserAuthSetting { UserId = userId };
            dbContext.UserAuthSettings.Add(settings);
        }

        settings.AccessTokenMinutes = request.AccessTokenMinutes;
        settings.RefreshTokenDays = request.RefreshTokenDays;
        settings.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return AuthSettingsResult.Success(MapUserResponse(
            request.AccessTokenMinutes,
            request.RefreshTokenDays,
            global));
    }

    private async Task<AuthLifetimeSettings> GetGlobalLifetimeAsync(CancellationToken cancellationToken)
    {
        var settings = await dbContext.AuthSettings
            .AsNoTracking()
            .Where(setting => setting.Key == AccessTokenMinutesKey || setting.Key == RefreshTokenDaysKey)
            .ToDictionaryAsync(setting => setting.Key, setting => setting.Value, cancellationToken);

        var accessTokenMinutes = ReadIntSetting(
            settings,
            AccessTokenMinutesKey,
            "Jwt:AccessTokenMinutes",
            DefaultAccessTokenMinutes);
        var refreshTokenDays = ReadIntSetting(
            settings,
            RefreshTokenDaysKey,
            "Auth:RefreshTokenDays",
            DefaultRefreshTokenDays);

        return new AuthLifetimeSettings(
            Clamp(accessTokenMinutes, MinAccessTokenMinutes, MaxAccessTokenMinutes),
            Clamp(refreshTokenDays, MinRefreshTokenDays, MaxRefreshTokenDays));
    }

    private async Task UpsertSettingAsync(string key, string value, CancellationToken cancellationToken)
    {
        var setting = await dbContext.AuthSettings
            .FirstOrDefaultAsync(item => item.Key == key, cancellationToken);

        if (setting is null)
        {
            setting = new AuthSetting { Key = key };
            dbContext.AuthSettings.Add(setting);
        }

        setting.Value = value;
        setting.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private int ReadIntSetting(
        IReadOnlyDictionary<string, string> settings,
        string settingKey,
        string configurationKey,
        int fallback)
    {
        if (settings.TryGetValue(settingKey, out var settingValue) &&
            int.TryParse(settingValue, out var settingNumber))
        {
            return settingNumber;
        }

        return int.TryParse(configuration[configurationKey], out var configurationNumber) && configurationNumber > 0
            ? configurationNumber
            : fallback;
    }

    private static string? ValidateLifetime(int accessTokenMinutes, int refreshTokenDays)
    {
        if (accessTokenMinutes is < MinAccessTokenMinutes or > MaxAccessTokenMinutes)
        {
            return $"Access token lifetime must be between {MinAccessTokenMinutes} and {MaxAccessTokenMinutes} minutes.";
        }

        if (refreshTokenDays is < MinRefreshTokenDays or > MaxRefreshTokenDays)
        {
            return $"Refresh session lifetime must be between {MinRefreshTokenDays} and {MaxRefreshTokenDays} days.";
        }

        return null;
    }

    private static AuthSettingsResponse MapGlobalResponse(AuthLifetimeSettings lifetime)
    {
        return new AuthSettingsResponse
        {
            AccessTokenMinutes = lifetime.AccessTokenMinutes,
            RefreshTokenDays = lifetime.RefreshTokenDays,
            MinAccessTokenMinutes = MinAccessTokenMinutes,
            MaxAccessTokenMinutes = MaxAccessTokenMinutes,
            MinRefreshTokenDays = MinRefreshTokenDays,
            MaxRefreshTokenDays = MaxRefreshTokenDays
        };
    }

    private static UserSessionSettingsResponse MapUserResponse(
        int accessTokenMinutes,
        int refreshTokenDays,
        AuthLifetimeSettings global)
    {
        return new UserSessionSettingsResponse
        {
            AccessTokenMinutes = accessTokenMinutes,
            RefreshTokenDays = refreshTokenDays,
            DefaultAccessTokenMinutes = global.AccessTokenMinutes,
            DefaultRefreshTokenDays = global.RefreshTokenDays,
            MinAccessTokenMinutes = MinAccessTokenMinutes,
            MaxAccessTokenMinutes = MaxAccessTokenMinutes,
            MinRefreshTokenDays = MinRefreshTokenDays,
            MaxRefreshTokenDays = MaxRefreshTokenDays
        };
    }

    private static int Clamp(int value, int min, int max)
    {
        return Math.Min(Math.Max(value, min), max);
    }
}
