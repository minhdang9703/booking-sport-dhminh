namespace BookingSport.Api.Services.Users;

public class UserResult<T>
{
    private UserResult(bool succeeded, T? value, string? error, UserResultStatus status)
    {
        Succeeded = succeeded;
        Value = value;
        Error = error;
        Status = status;
    }

    public bool Succeeded { get; }
    public T? Value { get; }
    public string? Error { get; }
    public UserResultStatus Status { get; }

    public static UserResult<T> Success(T value)
    {
        return new UserResult<T>(true, value, null, UserResultStatus.Success);
    }

    public static UserResult<T> NotFound(string error)
    {
        return new UserResult<T>(false, default, error, UserResultStatus.NotFound);
    }

    public static UserResult<T> Conflict(string error)
    {
        return new UserResult<T>(false, default, error, UserResultStatus.Conflict);
    }

    public static UserResult<T> BadRequest(string error)
    {
        return new UserResult<T>(false, default, error, UserResultStatus.BadRequest);
    }
}

public enum UserResultStatus
{
    Success,
    NotFound,
    Conflict,
    BadRequest
}
