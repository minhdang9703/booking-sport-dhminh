using BookingSport.Api.Enums;

namespace BookingSport.Api.Entities;

public class Court
{
    public Guid Id { get; set; }
    public Guid VenueId { get; set; }
    public Guid SportId { get; set; }
    public string Name { get; set; } = string.Empty;
    public CourtStatus Status { get; set; } = CourtStatus.Active;
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public Venue Venue { get; set; } = null!;
    public Sport Sport { get; set; } = null!;
    public ICollection<CourtSchedule> Schedules { get; set; } = new List<CourtSchedule>();
}
