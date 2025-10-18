using System.Diagnostics;

namespace SmartAuth.Infrastructure.Tracing;

public static class OtelTrace
{
    public static string CurrentTraceId() =>
        Activity.Current?.TraceId.ToHexString()
        ?? Activity.Current?.Id
        ?? Guid.NewGuid().ToString("N");
}