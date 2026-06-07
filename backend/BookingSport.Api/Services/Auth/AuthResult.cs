using BookingSport.Api.DTOs.Auth;

namespace BookingSport.Api.Services.Auth;

public class AuthResult
{
    private AuthResult(
        bool succeeded,
        AuthResponse? response,
        string? refreshToken,
        DateTimeOffset? refreshTokenExpiresAt,
        string? error)
    {
        Succeeded = succeeded;
        Response = response;
        RefreshToken = refreshToken;
        RefreshTokenExpiresAt = refreshTokenExpiresAt;
        Error = error;
    }

    public bool Succeeded { get; }
    public AuthResponse? Response { get; }
    public string? RefreshToken { get; }
    public DateTimeOffset? RefreshTokenExpiresAt { get; }
    public string? Error { get; }

    public static AuthResult Success(
        AuthResponse response,
        string? refreshToken = null,
        DateTimeOffset? refreshTokenExpiresAt = null)
    {
        return new AuthResult(true, response, refreshToken, refreshTokenExpiresAt, null);
    }

    public static AuthResult Failure(string error)
    {
        return new AuthResult(false, null, null, null, error);
    }
}
