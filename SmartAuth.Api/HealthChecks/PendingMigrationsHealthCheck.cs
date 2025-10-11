using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SmartAuth.Infrastructure;

namespace SmartAuth.Api.HealthChecks;

public sealed class PendingMigrationsHealthCheck(AuthDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var pending = await db.Database.GetPendingMigrationsAsync(cancellationToken);
            var enumerable = pending.ToList();
            var count = enumerable.Count();
            if (count == 0)
                return HealthCheckResult.Healthy("All migrations applied");

            return HealthCheckResult.Degraded(
                $"Pending migrations: {count}",
                data: new Dictionary<string, object?> { ["pending"] = enumerable.ToArray() }!);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Failed to check migrations", ex);
        }
    }
}