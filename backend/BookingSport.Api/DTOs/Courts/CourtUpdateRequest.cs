using BookingSport.Api.Enums;

namespace BookingSport.Api.DTOs.Courts;

public class CourtUpdateRequest
{
    public string Name { get; set; } = string.Empty;
    public CourtStatus Status { get; set; }
    public string? Description { get; set; }
}
