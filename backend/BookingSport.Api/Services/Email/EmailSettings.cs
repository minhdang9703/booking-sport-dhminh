namespace BookingSport.Api.Services.Email;

public sealed class EmailSettings
{
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromName { get; set; } = "Booking Sport";
    public string FromAddress { get; set; } = string.Empty;
    public bool UseSsl { get; set; } = true;
}
