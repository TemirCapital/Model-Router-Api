using System.Diagnostics;

namespace ModelRouterApi.Middleware;

/// <summary>
/// Captures per-request latency and writes a structured log entry.
/// In production, pair with Application Insights SDK for distributed tracing.
/// </summary>
public class RoutingTelemetryMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RoutingTelemetryMiddleware> _logger;

    public RoutingTelemetryMiddleware(
        RequestDelegate next,
        ILogger<RoutingTelemetryMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                            ?? Guid.NewGuid().ToString("N");

        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers.Append("X-Correlation-Id", correlationId);

        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            _logger.LogInformation(
                "Request {Method} {Path} responded {StatusCode} in {ElapsedMs}ms [CorrelationId={CorrelationId}]",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                sw.ElapsedMilliseconds,
                correlationId);
        }
    }
}
