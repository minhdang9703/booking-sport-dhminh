using BookingSport.Api.Enums;

namespace BookingSport.Api.DTOs.Courts;

public class CourtQueryParameters
{
    public CourtStatus? Status { get; set; }
    public string? CourtType { get; set; }
    public string? Keyword { get; set; }
}
