using BookingSport.Api.Enums;

namespace BookingSport.Api.DTOs.Courts;

public class CourtResponse
{
    public Guid Id { get; set; }
    public Guid VenueId { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public Guid SportId { get; set; }
    public string SportName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public CourtStatus Status { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
