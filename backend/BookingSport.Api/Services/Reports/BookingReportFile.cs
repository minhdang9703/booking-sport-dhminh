namespace BookingSport.Api.Services.Reports;

public sealed class BookingReportFile
{
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public byte[] Content { get; init; } = [];
}
