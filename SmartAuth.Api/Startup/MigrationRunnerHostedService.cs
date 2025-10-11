using Microsoft.EntityFrameworkCore;
using SmartAuth.Domain.Entities;
using SmartAuth.Infrastructure;
using SmartAuth.Infrastructure.Security;

namespace SmartAuth.Api.Startup;

public sealed class MigrationRunnerHostedService(IServiceProvider sp, ILogger<MigrationRunnerHostedService> log)
    : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        const int maxAttempts = 5;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

                log.LogInformation("Applying EF Core migrations (attempt {Attempt}/{Max})...", attempt, maxAttempts);
                await db.Database.MigrateAsync(ct);

                await SeedAsync(db, ct);

                log.LogInformation("Migrations applied successfully.");
                break;
            }
            catch (Exception ex) when (attempt < maxAttempts && !ct.IsCancellationRequested)
            {
                log.LogWarning(ex, "Migration attempt {Attempt} failed. Retrying in 3s...", attempt);
                await Task.Delay(TimeSpan.FromSeconds(3), ct);
            }
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    private static async Task SeedAsync(AuthDbContext db, CancellationToken ct)
    {
        if (!await db.Users.AnyAsync(ct))
        {
            var (hash, salt) = Passwords.Hash("Passw0rd!");
            db.Users.Add(new User { Email = "test@example.com", PasswordHash = hash, PasswordSalt = salt });
            await db.SaveChangesAsync(ct);
        }
    }
}