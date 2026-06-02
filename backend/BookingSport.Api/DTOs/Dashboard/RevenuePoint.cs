namespace BookingSport.Api.DTOs.Dashboard;

public class RevenuePoint
{
    public string Label { get; set; } = string.Empty;
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public decimal Revenue { get; set; }
    public int CompletedBookingCount { get; set; }
}
