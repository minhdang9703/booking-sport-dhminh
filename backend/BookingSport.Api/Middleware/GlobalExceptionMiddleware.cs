using BookingSport.Api.DTOs.Errors;
using BookingSport.Api.Logging;

namespace BookingSport.Api.Middleware;

public sealed class GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger,
    IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = context.TraceIdentifier;
        var sanitizedRequestBody = context.Items[ApiLoggingMiddleware.SanitizedRequestBodyItemKey] as string;

        logger.LogError(
            exception,
            "Unhandled exception while processing {Method} {Path}. TraceId={TraceId}, UserId={UserId}, UserRole={UserRole}, RouteValues={RouteValues}, RequestBody={RequestBody}",
            context.Request.Method,
            context.Request.Path.Value,
            traceId,
            context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            string.Join(",", context.User.Claims
                .Where(claim => claim.Type is System.Security.Claims.ClaimTypes.Role or "role")
                .Select(claim => claim.Value)),
            context.Request.RouteValues,
            sanitizedRequestBody);

        if (context.Response.HasStarted)
        {
            throw exception;
        }

        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Unexpected error",
            Message = "An unexpected error occurred while processing the request.",
            TraceId = traceId,
            Timestamp = DateTimeOffset.UtcNow,
            Detail = environment.IsDevelopment() || environment.IsEnvironment("IntegrationTests")
                ? exception.Message
                : null
        };

        await context.Response.WriteAsJsonAsync(response);
    }
}
