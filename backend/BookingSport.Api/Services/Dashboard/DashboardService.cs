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
            .Select(booking => new
            {
                booking.BookingDate,
                booking.TotalPrice
            })
            .ToListAsync(cancellationToken);

        var dailyGroups = bookings
            .GroupBy(booking => booking.BookingDate)
            .ToDictionary(
                group => group.Key,
                group => new RevenueDailyPoint
                {
                    Date = group.Key,
                    Revenue = group.Sum(booking => booking.TotalPrice),
                    CompletedBookingCount = group.Count()
                });

        var dailyRevenue = new List<RevenueDailyPoint>();

        for (var date = fromDate; date <= toDate; date = date.AddDays(1))
        {
            dailyRevenue.Add(dailyGroups.TryGetValue(date, out var point)
                ? point
                : new RevenueDailyPoint { Date = date });
        }

        var totalRevenue = dailyRevenue.Sum(point => point.Revenue);
        var completedBookingCount = dailyRevenue.Sum(point => point.CompletedBookingCount);
        var revenuePoints = BuildRevenuePoints(query.Period, fromDate, toDate, dailyRevenue);

        var response = new RevenueDashboardResponse
        {
            FromDate = fromDate,
            ToDate = toDate,
            TotalRevenue = totalRevenue,
            CompletedBookingCount = completedBookingCount,
            AverageBookingValue = completedBookingCount == 0 ? 0 : totalRevenue / completedBookingCount,
            Period = query.Period,
            RevenuePoints = revenuePoints,
            DailyRevenue = dailyRevenue
        };

        return DashboardResult<RevenueDashboardResponse>.Success(response);
    }

    private static IReadOnlyList<RevenuePoint> BuildRevenuePoints(
        RevenuePeriod period,
        DateOnly fromDate,
        DateOnly toDate,
        IReadOnlyList<RevenueDailyPoint> dailyRevenue)
    {
        return period switch
        {
            RevenuePeriod.Week => BuildWeeklyRevenuePoints(fromDate, toDate, dailyRevenue),
            RevenuePeriod.Month => BuildMonthlyRevenuePoints(fromDate, toDate, dailyRevenue),
            _ => dailyRevenue.Select(point => new RevenuePoint
            {
                Label = point.Date.ToString("yyyy-MM-dd"),
                FromDate = point.Date,
                ToDate = point.Date,
                Revenue = point.Revenue,
                CompletedBookingCount = point.CompletedBookingCount
            }).ToList()
        };
    }

    private static IReadOnlyList<RevenuePoint> BuildWeeklyRevenuePoints(
        DateOnly fromDate,
        DateOnly toDate,
        IReadOnlyList<RevenueDailyPoint> dailyRevenue)
    {
        var points = new List<RevenuePoint>();
        var weekStart = StartOfWeek(fromDate);

        while (weekStart <= toDate)
        {
            var weekEnd = weekStart.AddDays(6);
            var pointFrom = MaxDate(weekStart, fromDate);
            var pointTo = MinDate(weekEnd, toDate);
            var days = dailyRevenue.Where(point => point.Date >= pointFrom && point.Date <= pointTo).ToList();

            points.Add(new RevenuePoint
            {
                Label = $"{pointFrom:yyyy-MM-dd} - {pointTo:yyyy-MM-dd}",
                FromDate = pointFrom,
                ToDate = pointTo,
                Revenue = days.Sum(point => point.Revenue),
                CompletedBookingCount = days.Sum(point => point.CompletedBookingCount)
            });

            weekStart = weekStart.AddDays(7);
        }

        return points;
    }

    private static IReadOnlyList<RevenuePoint> BuildMonthlyRevenuePoints(
        DateOnly fromDate,
        DateOnly toDate,
        IReadOnlyList<RevenueDailyPoint> dailyRevenue)
    {
        var points = new List<RevenuePoint>();
        var monthStart = new DateOnly(fromDate.Year, fromDate.Month, 1);

        while (monthStart <= toDate)
        {
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            var pointFrom = MaxDate(monthStart, fromDate);
            var pointTo = MinDate(monthEnd, toDate);
            var days = dailyRevenue.Where(point => point.Date >= pointFrom && point.Date <= pointTo).ToList();

            points.Add(new RevenuePoint
            {
                Label = monthStart.ToString("yyyy-MM"),
                FromDate = pointFrom,
                ToDate = pointTo,
                Revenue = days.Sum(point => point.Revenue),
                CompletedBookingCount = days.Sum(point => point.CompletedBookingCount)
            });

            monthStart = monthStart.AddMonths(1);
        }

        return points;
    }

    private static DateOnly StartOfWeek(DateOnly date)
    {
        var diff = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;

        return date.AddDays(-diff);
    }

    private static DateOnly MaxDate(DateOnly first, DateOnly second)
    {
        return first > second ? first : second;
    }

    private static DateOnly MinDate(DateOnly first, DateOnly second)
    {
        return first < second ? first : second;
    }
}
