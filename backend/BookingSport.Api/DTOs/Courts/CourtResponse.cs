using BookingSport.Api.Enums;

namespace BookingSport.Api.DTOs.Courts;

public class CourtResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CourtType { get; set; } = string.Empty;
    public CourtStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
