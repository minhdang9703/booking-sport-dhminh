namespace BookingSport.Api.DTOs.PriceRules;

public class PriceRuleQueryParameters
{
    public DayOfWeek? DayOfWeek { get; set; }
    public bool? IsEnabled { get; set; }
}
