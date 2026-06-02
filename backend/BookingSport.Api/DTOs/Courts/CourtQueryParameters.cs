using BookingSport.Api.Enums;

namespace BookingSport.Api.DTOs.Courts;

public class CourtQueryParameters
{
    public Guid? VenueId { get; set; }
    public Guid? SportId { get; set; }
    public CourtStatus? Status { get; set; }
    public string? Keyword { get; set; }
}
