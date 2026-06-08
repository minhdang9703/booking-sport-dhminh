using BookingSport.Api.DTOs.Bookings;

namespace BookingSport.Api.Services.Reports;

public interface IAdminBookingReportService
{
    Task<BookingReportFile> ExportBookingsAsync(
        BookingQueryParameters query,
        CancellationToken cancellationToken);
}
