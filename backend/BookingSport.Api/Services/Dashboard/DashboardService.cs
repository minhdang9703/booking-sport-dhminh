using BookingSport.Api.Data;
using BookingSport.Api.DTOs.Dashboard;
using BookingSport.Api.Enums;
using Microsoft.EntityFrameworkCore;

namespace BookingSport.Api.Services.Dashboard;

public class DashboardService(AppDbContext dbContext) : IDashboardService
{
    public async Task<DashboardResult<RevenueDashboardResponse>> GetRevenueDashboardAsync(
        RevenueDashboardQuery query,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var fromDate = query.FromDate ?? today.AddDays(-30);
        var toDate = query.ToDate ?? today;

        if (fromDate > toDate)
        {
            return DashboardResult<RevenueDashboardResponse>.BadRequest("FromDate must be less than or equal to ToDate.");
        }

        var bookings = await dbContext.Bookings
            .AsNoTracking()
            .Where(booking =>
                booking.DeletedAt == null &&
                booking.Status == BookingStatus.Completed &&
                booking.BookingDate >= fromDate &&
                booking.BookingDate <= toDate)
            .GroupBy(booking => booking.BookingDate)
            .Select(group => new RevenueDailyPoint
            {
                Date = group.Key,
                Revenue = group.Sum(booking => booking.TotalPrice),
                CompletedBookingCount = group.Count()
            })
            .ToListAsync(cancellationToken);

        var revenueByDate = bookings.ToDictionary(point => point.Date);
        var dailyRevenue = new List<RevenueDailyPoint>();

        for (var date = fromDate; date <= toDate; date = date.AddDays(1))
        {
            dailyRevenue.Add(revenueByDate.TryGetValue(date, out var point)
                ? point
                : new RevenueDailyPoint { Date = date });
        }

        var totalRevenue = dailyRevenue.Sum(point => point.Revenue);
        var completedBookingCount = dailyRevenue.Sum(point => point.CompletedBookingCount);

        var response = new RevenueDashboardResponse
        {
            FromDate = fromDate,
            ToDate = toDate,
            TotalRevenue = totalRevenue,
            CompletedBookingCount = completedBookingCount,
            AverageBookingValue = completedBookingCount == 0 ? 0 : totalRevenue / completedBookingCount,
            DailyRevenue = dailyRevenue
        };

        return DashboardResult<RevenueDashboardResponse>.Success(response);
    }
}
