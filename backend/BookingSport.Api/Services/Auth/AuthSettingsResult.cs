using BookingSport.Api.DTOs.Auth;

namespace BookingSport.Api.Services.Auth;

public class AuthSettingsResult
{
    private AuthSettingsResult(
        bool succeeded,
        AuthSettingsResponse? authSettings,
        UserSessionSettingsResponse? userSessionSettings,
        string? error)
    {
        Succeeded = succeeded;
        AuthSettings = authSettings;
        UserSessionSettings = userSessionSettings;
        Error = error;
    }

    public bool Succeeded { get; }
    public AuthSettingsResponse? AuthSettings { get; }
    public UserSessionSettingsResponse? UserSessionSettings { get; }
    public string? Error { get; }

    public static AuthSettingsResult Success(AuthSettingsResponse response)
    {
        return new AuthSettingsResult(true, response, null, null);
    }

    public static AuthSettingsResult Success(UserSessionSettingsResponse response)
    {
        return new AuthSettingsResult(true, null, response, null);
    }

    public static AuthSettingsResult Failure(string error)
    {
        return new AuthSettingsResult(false, null, null, error);
    }
}
