using BookingSport.Api.Enums;

namespace BookingSport.Api.DTOs.Courts;

public class CourtCreateRequest
{
    public Guid VenueId { get; set; }
    public Guid SportId { get; set; }
    public string Name { get; set; } = string.Empty;
    public CourtStatus Status { get; set; } = CourtStatus.Active;
    public string? Description { get; set; }
}
