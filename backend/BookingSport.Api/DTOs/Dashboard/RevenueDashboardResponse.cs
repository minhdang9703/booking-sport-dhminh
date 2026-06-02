namespace BookingSport.Api.DTOs.Dashboard;

public class RevenueDashboardResponse
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public decimal TotalRevenue { get; set; }
    public int CompletedBookingCount { get; set; }
    public decimal AverageBookingValue { get; set; }
    public IReadOnlyList<RevenueDailyPoint> DailyRevenue { get; set; } = Array.Empty<RevenueDailyPoint>();
}
