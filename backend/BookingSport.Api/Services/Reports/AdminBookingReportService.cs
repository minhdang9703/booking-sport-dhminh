using BookingSport.Api.Data;
using BookingSport.Api.DTOs.Bookings;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace BookingSport.Api.Services.Reports;

public sealed class AdminBookingReportService(AppDbContext dbContext) : IAdminBookingReportService
{
    public async Task<BookingReportFile> ExportBookingsAsync(
        BookingQueryParameters query,
        CancellationToken cancellationToken)
    {
        var bookingsQuery = dbContext.Bookings
            .AsNoTracking()
            .Include(booking => booking.User)
            .Include(booking => booking.Court)
            .Where(booking => booking.DeletedAt == null);

        if (query.FromDate.HasValue)
        {
            bookingsQuery = bookingsQuery.Where(booking => booking.BookingDate >= query.FromDate.Value);
        }

        if (query.ToDate.HasValue)
        {
            bookingsQuery = bookingsQuery.Where(booking => booking.BookingDate <= query.ToDate.Value);
        }

        if (query.CourtId.HasValue)
        {
            bookingsQuery = bookingsQuery.Where(booking => booking.CourtId == query.CourtId.Value);
        }

        if (query.Status.HasValue)
        {
            bookingsQuery = bookingsQuery.Where(booking => booking.Status == query.Status.Value);
        }

        var bookings = await bookingsQuery
            .OrderByDescending(booking => booking.BookingDate)
            .ThenBy(booking => booking.StartTime)
            .ToListAsync(cancellationToken);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Bookings");

        var headers = new[]
        {
            "Booking Id",
            "Customer",
            "Email",
            "Phone",
            "Court",
            "Booking Date",
            "Start Time",
            "End Time",
            "Status",
            "Payment Type",
            "Hourly Price",
            "Total Price",
            "Note",
            "Created At"
        };

        for (var index = 0; index < headers.Length; index++)
        {
            worksheet.Cell(1, index + 1).Value = headers[index];
        }

        var headerRange = worksheet.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9EAD3");
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        for (var rowIndex = 0; rowIndex < bookings.Count; rowIndex++)
        {
            var booking = bookings[rowIndex];
            var row = rowIndex + 2;

            worksheet.Cell(row, 1).Value = booking.Id.ToString();
            worksheet.Cell(row, 2).Value = booking.User.FullName;
            worksheet.Cell(row, 3).Value = booking.User.Email;
            worksheet.Cell(row, 4).Value = booking.User.PhoneNumber;
            worksheet.Cell(row, 5).Value = booking.Court.Name;
            worksheet.Cell(row, 6).Value = booking.BookingDate.ToDateTime(TimeOnly.MinValue);
            worksheet.Cell(row, 7).Value = booking.StartTime.ToString("HH:mm");
            worksheet.Cell(row, 8).Value = booking.EndTime.ToString("HH:mm");
            worksheet.Cell(row, 9).Value = booking.Status.ToString();
            worksheet.Cell(row, 10).Value = booking.PaymentType.ToString();
            worksheet.Cell(row, 11).Value = booking.HourlyPriceSnapshot;
            worksheet.Cell(row, 12).Value = booking.TotalPrice;
            worksheet.Cell(row, 13).Value = booking.Note ?? string.Empty;
            worksheet.Cell(row, 14).Value = booking.CreatedAt.UtcDateTime;
        }

        worksheet.Column(6).Style.DateFormat.Format = "yyyy-mm-dd";
        worksheet.Column(11).Style.NumberFormat.Format = "#,##0";
        worksheet.Column(12).Style.NumberFormat.Format = "#,##0";
        worksheet.Column(14).Style.DateFormat.Format = "yyyy-mm-dd hh:mm";
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return new BookingReportFile
        {
            FileName = $"booking-report-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.xlsx",
            Content = stream.ToArray()
        };
    }
}
