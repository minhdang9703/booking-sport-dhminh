using BookingSport.Api.Enums;

namespace BookingSport.Api.DTOs.Courts;

public class CourtCreateRequest
{
    public string Name { get; set; } = string.Empty;
    public string CourtType { get; set; } = string.Empty;
    public CourtStatus Status { get; set; } = CourtStatus.Active;
}
