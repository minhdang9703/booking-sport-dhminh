namespace BookingSport.Api.DTOs.Auth;

public class AuthSettingsResponse
{
    public int AccessTokenMinutes { get; set; }
    public int RefreshTokenDays { get; set; }
    public int MinAccessTokenMinutes { get; set; }
    public int MaxAccessTokenMinutes { get; set; }
    public int MinRefreshTokenDays { get; set; }
    public int MaxRefreshTokenDays { get; set; }
}
