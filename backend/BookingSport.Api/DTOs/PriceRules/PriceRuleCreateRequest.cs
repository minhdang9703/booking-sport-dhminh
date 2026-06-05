namespace BookingSport.Api.DTOs.PriceRules;

public class PriceRuleCreateRequest
{
    public string Name { get; set; } = string.Empty;
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public decimal HourlyPrice { get; set; }
    public bool IsEnabled { get; set; } = true;
}
