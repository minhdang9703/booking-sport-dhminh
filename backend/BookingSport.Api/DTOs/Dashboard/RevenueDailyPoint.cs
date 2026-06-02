namespace BookingSport.Api.DTOs.Dashboard;

public class RevenueDailyPoint
{
    public DateOnly Date { get; set; }
    public decimal Revenue { get; set; }
    public int CompletedBookingCount { get; set; }
}
