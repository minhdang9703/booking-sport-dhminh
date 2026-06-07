using BookingSport.Api.DTOs.Auth;

namespace BookingSport.Api.Services.Auth;

public interface IAuthSettingsService
{
    Task<AuthLifetimeSettings> GetEffectiveLifetimeAsync(Guid? userId, CancellationToken cancellationToken);
    Task<AuthSettingsResponse> GetGlobalSettingsAsync(CancellationToken cancellationToken);
    Task<AuthSettingsResult> UpdateGlobalSettingsAsync(
        AuthSettingsUpdateRequest request,
        CancellationToken cancellationToken);
    Task<UserSessionSettingsResponse> GetUserSessionSettingsAsync(
        Guid userId,
        CancellationToken cancellationToken);
    Task<AuthSettingsResult> UpdateUserSessionSettingsAsync(
        Guid userId,
        UserSessionSettingsUpdateRequest request,
        CancellationToken cancellationToken);
}
