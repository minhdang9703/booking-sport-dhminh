namespace BookingSport.Api.DTOs.Sports;

public class SportCreateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
