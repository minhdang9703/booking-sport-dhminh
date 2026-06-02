namespace BookingSport.Api.DTOs.Venues;

public class VenueCreateRequest
{
    public Guid OwnerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? Description { get; set; }
}
