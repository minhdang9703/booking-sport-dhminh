namespace BookingSport.Api.Services.Dashboard;

public class DashboardResult<T>
{
    private DashboardResult(bool succeeded, T? value, string? error)
    {
        Succeeded = succeeded;
        Value = value;
        Error = error;
    }

    public bool Succeeded { get; }
    public T? Value { get; }
    public string? Error { get; }

    public static DashboardResult<T> Success(T value)
    {
        return new DashboardResult<T>(true, value, null);
    }

    public static DashboardResult<T> BadRequest(string error)
    {
        return new DashboardResult<T>(false, default, error);
    }
}
