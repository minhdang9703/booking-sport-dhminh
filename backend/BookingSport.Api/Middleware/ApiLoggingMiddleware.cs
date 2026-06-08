using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using BookingSport.Api.Logging;
using Microsoft.Extensions.Options;

namespace BookingSport.Api.Middleware;

public sealed class ApiLoggingMiddleware(
    RequestDelegate next,
    ILogger<ApiLoggingMiddleware> logger,
    ILogSanitizer sanitizer,
    IOptions<ApiLoggingOptions> options)
{
    public const string SanitizedRequestBodyItemKey = "BookingSport.SanitizedRequestBody";

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var sanitizedRequestBody = await TryReadSanitizedRequestBodyAsync(context);

        if (!string.IsNullOrWhiteSpace(sanitizedRequestBody))
        {
            context.Items[SanitizedRequestBodyItemKey] = sanitizedRequestBody;
        }

        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();
            LogRequest(context, stopwatch.ElapsedMilliseconds, sanitizedRequestBody);
        }
    }

    private async Task<string?> TryReadSanitizedRequestBodyAsync(HttpContext context)
    {
        var apiLoggingOptions = options.Value;

        if (!apiLoggingOptions.EnableRequestBodyLogging ||
            !ShouldLogRequestBody(context, apiLoggingOptions))
        {
            return null;
        }

        context.Request.EnableBuffering();

        var maxBytes = Math.Max(apiLoggingOptions.MaxBodySizeBytes, 1024);
        using var reader = new StreamReader(
            context.Request.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            bufferSize: 1024,
            leaveOpen: true);
        var buffer = new char[maxBytes + 1];
        var readCount = await reader.ReadBlockAsync(buffer, 0, buffer.Length);
        context.Request.Body.Position = 0;

        if (readCount == 0)
        {
            return null;
        }

        var body = new string(buffer, 0, Math.Min(readCount, maxBytes));
        var sanitized = sanitizer.SanitizeText(body);

        return readCount > maxBytes ? $"{sanitized}...<truncated>" : sanitized;
    }

    private static bool ShouldLogRequestBody(HttpContext context, ApiLoggingOptions options)
    {
        if (!HttpMethods.IsPost(context.Request.Method) &&
            !HttpMethods.IsPut(context.Request.Method) &&
            !HttpMethods.IsPatch(context.Request.Method) &&
            !HttpMethods.IsDelete(context.Request.Method))
        {
            return false;
        }

        if (!context.Request.ContentLength.GetValueOrDefault().Equals(0) &&
            context.Request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) != true)
        {
            return false;
        }

        return !options.ExcludedPaths.Any(path =>
            context.Request.Path.StartsWithSegments(path, StringComparison.OrdinalIgnoreCase));
    }

    private void LogRequest(HttpContext context, long elapsedMilliseconds, string? sanitizedRequestBody)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userRole = string.Join(",", context.User.Claims
            .Where(claim => claim.Type is ClaimTypes.Role or "role")
            .Select(claim => claim.Value));

        logger.LogInformation(
            "API request {Method} {Path}{QueryString} responded {StatusCode} in {ElapsedMilliseconds} ms. TraceId={TraceId}, UserId={UserId}, UserRole={UserRole}, RequestBody={RequestBody}",
            context.Request.Method,
            context.Request.Path.Value,
            sanitizer.SanitizeText(context.Request.QueryString.Value),
            context.Response.StatusCode,
            elapsedMilliseconds,
            context.TraceIdentifier,
            string.IsNullOrWhiteSpace(userId) ? null : userId,
            string.IsNullOrWhiteSpace(userRole) ? null : userRole,
            sanitizedRequestBody);
    }
}
