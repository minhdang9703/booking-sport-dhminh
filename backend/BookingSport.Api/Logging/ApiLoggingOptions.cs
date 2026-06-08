namespace BookingSport.Api.Logging;

public sealed class ApiLoggingOptions
{
    public bool EnableRequestBodyLogging { get; set; } = true;
    public int MaxBodySizeBytes { get; set; } = 16 * 1024;
    public string[] ExcludedPaths { get; set; } =
    [
        "/api/auth/login",
        "/api/auth/register",
        "/api/auth/refresh",
        "/api/auth/logout"
    ];
}
