namespace BookingSport.Api.DTOs.Errors;

public sealed class ErrorResponse
{
    public int Status { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string TraceId { get; init; } = string.Empty;
    public DateTimeOffset Timestamp { get; init; }
    public string? Detail { get; init; }
}
