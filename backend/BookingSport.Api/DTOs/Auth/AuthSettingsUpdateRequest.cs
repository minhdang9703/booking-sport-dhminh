namespace BookingSport.Api.DTOs.Auth;

public class AuthSettingsUpdateRequest
{
    public int AccessTokenMinutes { get; set; }
    public int RefreshTokenDays { get; set; }
}
