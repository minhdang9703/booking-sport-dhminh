using BookingSport.Api.DTOs.Bookings;
using BookingSport.Api.Enums;
using BookingSport.Api.Services.Reports;
using BookingSport.Api.Tests.TestSupport;
using ClosedXML.Excel;
using FluentAssertions;

namespace BookingSport.Api.Tests.Services.Reports;

public class AdminBookingReportServiceTests
{
    private static readonly DateOnly Monday = new(2026, 6, 8);

    [Fact]
    public async Task ExportBookingsAsync_WithFilters_CreatesWorkbookWithMatchingRows()
    {
        await using var db = await TestDb.CreateAsync();
        var user = TestData.User();
        var court = TestData.Court();
        var otherCourt = TestData.Court(name: "Court B");
        db.Context.Users.Add(user);
        db.Context.Courts.AddRange(court, otherCourt);
        db.Context.Bookings.AddRange(
            TestData.Booking(userId: user.Id, courtId: court.Id, bookingDate: Monday, status: BookingStatus.Completed),
            TestData.Booking(userId: user.Id, courtId: otherCourt.Id, bookingDate: Monday, status: BookingStatus.Pending),
            TestData.Booking(userId: user.Id, courtId: court.Id, bookingDate: Monday.AddDays(1), status: BookingStatus.Completed));
        await db.Context.SaveChangesAsync();
        var service = new AdminBookingReportService(db.Context);

        var report = await service.ExportBookingsAsync(new BookingQueryParameters
        {
            FromDate = Monday,
            ToDate = Monday,
            CourtId = court.Id,
            Status = BookingStatus.Completed
        }, CancellationToken.None);

        report.FileName.Should().StartWith("booking-report-").And.EndWith(".xlsx");
        report.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        report.Content.Should().NotBeEmpty();

        using var stream = new MemoryStream(report.Content);
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheet("Bookings");

        worksheet.Cell(1, 1).GetString().Should().Be("Booking Id");
        worksheet.Cell(1, 12).GetString().Should().Be("Total Price");
        worksheet.Cell(2, 5).GetString().Should().Be(court.Name);
        worksheet.Cell(2, 9).GetString().Should().Be(BookingStatus.Completed.ToString());
        worksheet.Cell(3, 1).IsEmpty().Should().BeTrue();
    }
}
