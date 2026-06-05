namespace BookingSport.Api.Services.PriceRules;

public class PriceRuleResult<T>
{
    private PriceRuleResult(bool succeeded, T? value, string? error, PriceRuleResultStatus status)
    {
        Succeeded = succeeded;
        Value = value;
        Error = error;
        Status = status;
    }

    public bool Succeeded { get; }
    public T? Value { get; }
    public string? Error { get; }
    public PriceRuleResultStatus Status { get; }

    public static PriceRuleResult<T> Success(T value)
    {
        return new PriceRuleResult<T>(true, value, null, PriceRuleResultStatus.Success);
    }

    public static PriceRuleResult<T> BadRequest(string error)
    {
        return new PriceRuleResult<T>(false, default, error, PriceRuleResultStatus.BadRequest);
    }

    public static PriceRuleResult<T> NotFound(string error)
    {
        return new PriceRuleResult<T>(false, default, error, PriceRuleResultStatus.NotFound);
    }

    public static PriceRuleResult<T> Conflict(string error)
    {
        return new PriceRuleResult<T>(false, default, error, PriceRuleResultStatus.Conflict);
    }
}

public enum PriceRuleResultStatus
{
    Success,
    BadRequest,
    Conflict,
    NotFound
}
