using BookingSport.Api.Enums;

namespace BookingSport.Api.Entities;

public class Court
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CourtType { get; set; } = string.Empty;
    public CourtStatus Status { get; set; } = CourtStatus.Active;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
