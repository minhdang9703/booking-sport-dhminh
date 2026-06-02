namespace BookingSport.Api.DTOs.Sports;

public class SportUpdateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
