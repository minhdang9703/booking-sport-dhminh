namespace BookingSport.Api.DTOs.Auth;

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public CurrentUserResponse User { get; set; } = new();
}
