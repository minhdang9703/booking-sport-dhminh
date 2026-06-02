namespace BookingSport.Api.DTOs.CourtSchedules;

public class CourtScheduleQueryParameters
{
    public Guid? CourtId { get; set; }
    public DayOfWeek? DayOfWeek { get; set; }
    public bool? IsAvailable { get; set; }
}
