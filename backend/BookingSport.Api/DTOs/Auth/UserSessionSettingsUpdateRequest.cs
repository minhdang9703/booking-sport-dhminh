namespace BookingSport.Api.DTOs.Auth;

public class UserSessionSettingsUpdateRequest
{
    public int AccessTokenMinutes { get; set; }
    public int RefreshTokenDays { get; set; }
}
