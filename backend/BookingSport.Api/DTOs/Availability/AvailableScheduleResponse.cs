namespace BookingSport.Api.DTOs.Availability;

public class AvailableScheduleResponse
{
    public Guid ScheduleId { get; set; }
    public Guid CourtId { get; set; }
    public string CourtName { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; }
}
