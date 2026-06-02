namespace BookingSport.Api.DTOs.Venues;

public class VenueUpdateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? Description { get; set; }
}
