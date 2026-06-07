namespace BookingSport.Api.Services.Auth;

public sealed record AuthLifetimeSettings(int AccessTokenMinutes, int RefreshTokenDays);
