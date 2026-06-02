namespace BookingSport.Api.Services.Courts;

public class CourtResult<T>
{
    private CourtResult(bool succeeded, T? value, string? error, CourtResultStatus status)
    {
        Succeeded = succeeded;
        Value = value;
        Error = error;
        Status = status;
    }

    public bool Succeeded { get; }
    public T? Value { get; }
    public string? Error { get; }
    public CourtResultStatus Status { get; }

    public static CourtResult<T> Success(T value)
    {
        return new CourtResult<T>(true, value, null, CourtResultStatus.Success);
    }

    public static CourtResult<T> NotFound(string error)
    {
        return new CourtResult<T>(false, default, error, CourtResultStatus.NotFound);
    }

    public static CourtResult<T> Conflict(string error)
    {
        return new CourtResult<T>(false, default, error, CourtResultStatus.Conflict);
    }

    public static CourtResult<T> BadRequest(string error)
    {
        return new CourtResult<T>(false, default, error, CourtResultStatus.BadRequest);
    }
}

public enum CourtResultStatus
{
    Success,
    NotFound,
    Conflict,
    BadRequest
}
