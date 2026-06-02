namespace BookingSport.Api.Entities;

public class CourtSchedule
{
    public Guid Id { get; set; }
    public Guid CourtId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public Court Court { get; set; } = null!;
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
