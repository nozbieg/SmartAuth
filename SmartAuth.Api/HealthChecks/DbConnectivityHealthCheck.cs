using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SmartAuth.Infrastructure;

namespace SmartAuth.Api.HealthChecks;

public sealed class DbConnectivityHealthCheck(AuthDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            var can = await db.Database.CanConnectAsync(ct);
            IReadOnlyDictionary<string, object> info = new Dictionary<string, object>
            {
                ["provider"] = db.Database.ProviderName ?? string.Empty,
                ["database"] = db.Database.GetDbConnection().Database
            };
            return can
                ? HealthCheckResult.Healthy("DB reachable", info)
                : HealthCheckResult.Unhealthy("DB unreachable");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("DB check failed", ex);
        }
    }
}