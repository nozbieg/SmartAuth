using SmartAuth.Domain.Interfaces;
using SmartAuth.Infrastructure.Configurations;

namespace SmartAuth.Infrastructure;

public class AuthDbContext(DbContextOptions<AuthDbContext> options, TimeProvider timeProvider) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<IAuditEntity>();
        var now = timeProvider.GetUtcNow().UtcDateTime;

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = now;
                    entry.Entity.UpdatedAtUtc = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAtUtc = now;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");
        ModelBuilderAuditableExtensions.ApplyAuditableConfigurations(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuthDbContext).Assembly);
    }
}