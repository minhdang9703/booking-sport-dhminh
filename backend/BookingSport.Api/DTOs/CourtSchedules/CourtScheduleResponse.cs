namespace BookingSport.Api.DTOs.CourtSchedules;

public class CourtScheduleResponse
{
    public Guid Id { get; set; }
    public Guid CourtId { get; set; }
    public string CourtName { get; set; } = string.Empty;
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
