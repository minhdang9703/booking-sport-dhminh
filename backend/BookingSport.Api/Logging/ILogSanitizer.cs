namespace BookingSport.Api.Logging;

public interface ILogSanitizer
{
    string SanitizeText(string? value);
}
