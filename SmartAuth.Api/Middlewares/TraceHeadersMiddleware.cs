using SmartAuth.Infrastructure.Tracing;

namespace SmartAuth.Api.Middlewares;

public sealed class TraceHeadersMiddleware
{
    private readonly RequestDelegate _next;
    public TraceHeadersMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx)
    {
        await _next(ctx);

        var traceId = OtelTrace.CurrentTraceId();
        var spanId  = OtelTrace.CurrentSpanId();
        var parent  = OtelTrace.CurrentTraceParent();

        ctx.Response.Headers["X-Trace-Id"] = traceId;
        if (!string.IsNullOrEmpty(spanId))
            ctx.Response.Headers["X-Span-Id"] = spanId;
        if (!string.IsNullOrEmpty(parent))
            ctx.Response.Headers["traceparent"] = parent; 
    }
}