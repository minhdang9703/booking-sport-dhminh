using BookingSport.Api.DTOs.Auth;

namespace BookingSport.Api.Services.Auth;

public class AuthResult
{
    private AuthResult(bool succeeded, AuthResponse? response, string? error)
    {
        Succeeded = succeeded;
        Response = response;
        Error = error;
    }

    public bool Succeeded { get; }
    public AuthResponse? Response { get; }
    public string? Error { get; }

    public static AuthResult Success(AuthResponse response)
    {
        return new AuthResult(true, response, null);
    }

    public static AuthResult Failure(string error)
    {
        return new AuthResult(false, null, error);
    }
}
