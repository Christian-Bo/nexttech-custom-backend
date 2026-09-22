using System.Diagnostics;

namespace NextTech.Api.Middleware;

/// <summary>
/// Development-oriented request correlation without logging request bodies,
/// Authorization headers, passwords, JWTs, QR credentials or biometric data.
/// </summary>
public sealed class RequestDiagnosticsMiddleware(
    RequestDelegate next,
    ILogger<RequestDiagnosticsMiddleware> logger,
    IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var traceId = context.TraceIdentifier;
        context.Response.Headers["X-Trace-Id"] = traceId;

        if (environment.IsDevelopment())
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers["X-Elapsed-Ms"] = stopwatch.ElapsedMilliseconds.ToString();
                return Task.CompletedTask;
            });
        }

        logger.LogInformation(
            "HTTP started. Method={Method} Path={Path} TraceId={TraceId}",
            context.Request.Method,
            context.Request.Path,
            traceId);

        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();
            logger.LogInformation(
                "HTTP completed. Method={Method} Path={Path} Status={StatusCode} ElapsedMs={ElapsedMs} TraceId={TraceId}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                traceId);
        }
    }
}
