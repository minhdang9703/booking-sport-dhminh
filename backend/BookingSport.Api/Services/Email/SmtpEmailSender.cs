using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace BookingSport.Api.Services.Email;

public sealed class SmtpEmailSender(
    IOptions<EmailSettings> settings,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var emailSettings = settings.Value;

        if (string.IsNullOrWhiteSpace(emailSettings.SmtpHost) ||
            string.IsNullOrWhiteSpace(emailSettings.FromAddress))
        {
            logger.LogWarning(
                "Email settings are incomplete. Booking confirmation email to {ToEmail} was skipped.",
                message.ToEmail);
            return;
        }

        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(new MailboxAddress(emailSettings.FromName, emailSettings.FromAddress));
        mimeMessage.To.Add(new MailboxAddress(message.ToName, message.ToEmail));
        mimeMessage.Subject = message.Subject;

        var bodyBuilder = new BodyBuilder
        {
            TextBody = message.TextBody,
            HtmlBody = string.IsNullOrWhiteSpace(message.HtmlBody) ? null : message.HtmlBody
        };
        mimeMessage.Body = bodyBuilder.ToMessageBody();

        using var smtpClient = new SmtpClient();
        var secureSocketOptions = emailSettings.UseSsl
            ? SecureSocketOptions.StartTlsWhenAvailable
            : SecureSocketOptions.Auto;

        await smtpClient.ConnectAsync(
            emailSettings.SmtpHost,
            emailSettings.SmtpPort,
            secureSocketOptions,
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(emailSettings.Username))
        {
            await smtpClient.AuthenticateAsync(
                emailSettings.Username,
                emailSettings.Password,
                cancellationToken);
        }

        await smtpClient.SendAsync(mimeMessage, cancellationToken);
        await smtpClient.DisconnectAsync(true, cancellationToken);
    }
}
