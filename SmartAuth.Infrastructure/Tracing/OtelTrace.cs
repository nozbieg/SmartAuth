using System.Diagnostics;

namespace SmartAuth.Infrastructure.Tracing;

public static class OtelTrace
{
    public static string CurrentTraceId() =>
        Activity.Current?.TraceId.ToHexString()
        ?? Activity.Current?.Id 
        ?? Guid.NewGuid().ToString("N");

    public static string? CurrentSpanId() =>
        Activity.Current?.SpanId.ToHexString();

    public static string? CurrentTraceParent() =>
        Activity.Current is { } a
            ? $"00-{a.TraceId.ToHexString()}-{a.SpanId.ToHexString()}-{(a.Recorded ? "01" : "00")}"
            : null;
}