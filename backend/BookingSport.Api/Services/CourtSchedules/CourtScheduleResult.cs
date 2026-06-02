namespace BookingSport.Api.Services.CourtSchedules;

public class CourtScheduleResult<T>
{
    private CourtScheduleResult(bool succeeded, T? value, string? error, CourtScheduleResultStatus status)
    {
        Succeeded = succeeded;
        Value = value;
        Error = error;
        Status = status;
    }

    public bool Succeeded { get; }
    public T? Value { get; }
    public string? Error { get; }
    public CourtScheduleResultStatus Status { get; }

    public static CourtScheduleResult<T> Success(T value)
    {
        return new CourtScheduleResult<T>(true, value, null, CourtScheduleResultStatus.Success);
    }

    public static CourtScheduleResult<T> BadRequest(string error)
    {
        return new CourtScheduleResult<T>(false, default, error, CourtScheduleResultStatus.BadRequest);
    }

    public static CourtScheduleResult<T> Conflict(string error)
    {
        return new CourtScheduleResult<T>(false, default, error, CourtScheduleResultStatus.Conflict);
    }

    public static CourtScheduleResult<T> NotFound(string error)
    {
        return new CourtScheduleResult<T>(false, default, error, CourtScheduleResultStatus.NotFound);
    }
}

public enum CourtScheduleResultStatus
{
    Success,
    BadRequest,
    Conflict,
    NotFound
}
