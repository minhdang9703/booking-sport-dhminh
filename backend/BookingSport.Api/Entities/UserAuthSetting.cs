namespace BookingSport.Api.Entities;

public class UserAuthSetting
{
    public Guid UserId { get; set; }
    public int? AccessTokenMinutes { get; set; }
    public int? RefreshTokenDays { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public User User { get; set; } = null!;
}
