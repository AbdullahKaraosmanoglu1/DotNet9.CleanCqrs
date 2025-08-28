using Serilog.Context;

namespace DotNet9.Api.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task Invoke(HttpContext context)
    {
        // İstekten al ya da üret
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var h) && !string.IsNullOrWhiteSpace(h)
            ? h.ToString()
            : Guid.NewGuid().ToString("n");

        // Response'a yaz (müşteri görebilsin)
        context.Response.Headers[HeaderName] = correlationId;

        // Serilog scope
        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("ClientIp", context.Connection.RemoteIpAddress?.ToString()))
        using (LogContext.PushProperty("UserAgent", context.Request.Headers.UserAgent.ToString()))
        using (LogContext.PushProperty("Route", $"{context.Request.Method} {context.Request.Path}"))
        {
            await next(context);
        }
    }
}
