namespace BookingSport.Api.DTOs.Auth;

public class UserSessionSettingsResponse
{
    public int AccessTokenMinutes { get; set; }
    public int RefreshTokenDays { get; set; }
    public int DefaultAccessTokenMinutes { get; set; }
    public int DefaultRefreshTokenDays { get; set; }
    public int MinAccessTokenMinutes { get; set; }
    public int MaxAccessTokenMinutes { get; set; }
    public int MinRefreshTokenDays { get; set; }
    public int MaxRefreshTokenDays { get; set; }
}
