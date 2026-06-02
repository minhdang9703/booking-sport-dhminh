namespace BookingSport.Api.DTOs.CourtSchedules;

public class CourtScheduleUpdateRequest
{
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; }
}
