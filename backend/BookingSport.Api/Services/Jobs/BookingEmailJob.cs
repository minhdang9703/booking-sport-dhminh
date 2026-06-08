using System.Globalization;
using BookingSport.Api.Data;
using BookingSport.Api.Services.Email;
using Microsoft.EntityFrameworkCore;

namespace BookingSport.Api.Services.Jobs;

public sealed class BookingEmailJob(
    AppDbContext dbContext,
    IEmailSender emailSender,
    ILogger<BookingEmailJob> logger) : IBookingEmailJob
{
    public async Task SendBookingConfirmationAsync(Guid bookingId, CancellationToken cancellationToken)
    {
        var booking = await dbContext.Bookings
            .AsNoTracking()
            .Include(item => item.User)
            .Include(item => item.Court)
            .FirstOrDefaultAsync(item => item.Id == bookingId && item.DeletedAt == null, cancellationToken);

        if (booking is null)
        {
            logger.LogWarning("Booking confirmation email was skipped because booking {BookingId} was not found.", bookingId);
            return;
        }

        var culture = CultureInfo.GetCultureInfo("vi-VN");
        var date = booking.BookingDate.ToString("dd/MM/yyyy", culture);
        var timeRange = $"{booking.StartTime:HH\\:mm} - {booking.EndTime:HH\\:mm}";
        var totalPrice = booking.TotalPrice.ToString("N0", culture);

        var textBody =
            $"Xin chao {booking.User.FullName},\n\n" +
            "Dat san cua ban da duoc ghi nhan.\n" +
            $"Ma booking: {booking.Id}\n" +
            $"San: {booking.Court.Name}\n" +
            $"Ngay: {date}\n" +
            $"Gio: {timeRange}\n" +
            $"Trang thai: {booking.Status}\n" +
            $"Tong tien: {totalPrice} VND\n\n" +
            "Cam on ban da su dung Booking Sport.";

        var htmlBody =
            $"<p>Xin chao {booking.User.FullName},</p>" +
            "<p>Dat san cua ban da duoc ghi nhan.</p>" +
            "<ul>" +
            $"<li><strong>Ma booking:</strong> {booking.Id}</li>" +
            $"<li><strong>San:</strong> {booking.Court.Name}</li>" +
            $"<li><strong>Ngay:</strong> {date}</li>" +
            $"<li><strong>Gio:</strong> {timeRange}</li>" +
            $"<li><strong>Trang thai:</strong> {booking.Status}</li>" +
            $"<li><strong>Tong tien:</strong> {totalPrice} VND</li>" +
            "</ul>" +
            "<p>Cam on ban da su dung Booking Sport.</p>";

        await emailSender.SendAsync(new EmailMessage
        {
            ToEmail = booking.User.Email,
            ToName = booking.User.FullName,
            Subject = $"Xac nhan booking {booking.Court.Name} ngay {date}",
            TextBody = textBody,
            HtmlBody = htmlBody
        }, cancellationToken);

        logger.LogInformation("Booking confirmation email was sent for booking {BookingId}.", booking.Id);
    }
}
