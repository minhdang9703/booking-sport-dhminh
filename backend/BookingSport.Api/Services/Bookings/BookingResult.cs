namespace BookingSport.Api.Services.Bookings;

public class BookingResult<T>
{
    private BookingResult(bool succeeded, T? value, string? error, BookingResultStatus status)
    {
        Succeeded = succeeded;
        Value = value;
        Error = error;
        Status = status;
    }

    public bool Succeeded { get; }
    public T? Value { get; }
    public string? Error { get; }
    public BookingResultStatus Status { get; }

    public static BookingResult<T> Success(T value)
    {
        return new BookingResult<T>(true, value, null, BookingResultStatus.Success);
    }

    public static BookingResult<T> NotFound(string error)
    {
        return new BookingResult<T>(false, default, error, BookingResultStatus.NotFound);
    }

    public static BookingResult<T> Conflict(string error)
    {
        return new BookingResult<T>(false, default, error, BookingResultStatus.Conflict);
    }

    public static BookingResult<T> BadRequest(string error)
    {
        return new BookingResult<T>(false, default, error, BookingResultStatus.BadRequest);
    }
}

public enum BookingResultStatus
{
    Success,
    NotFound,
    Conflict,
    BadRequest
}
