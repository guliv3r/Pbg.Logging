using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Pbg.Logging;

public class PbgLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public PbgLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ILogger<PbgLoggingMiddleware> logger)
    {
        var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? context.User?.FindFirst("sub")?.Value
                     ?? "Anonymous";

        var traceId = context.TraceIdentifier;

        var scopeData = new Dictionary<string, object>
        {
            { "UserId", userId },
            { "TraceId", traceId }
        };

        using (logger.BeginScope(scopeData))
        {
            await _next(context);
        }
    }
}