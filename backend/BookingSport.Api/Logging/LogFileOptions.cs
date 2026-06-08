namespace BookingSport.Api.Logging;

public sealed class LogFileOptions
{
    public bool Enabled { get; set; } = true;
    public string FolderPath { get; set; } = "logs";
    public string FileNamePattern { get; set; } = "booking-sport-api-.log";
    public int RetainedFileCountLimit { get; set; } = 14;
}
