using Microsoft.EntityFrameworkCore;
using SmartAuth.Domain.Entities;

namespace SmartAuth.Infrastructure;

public class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserAuthenticator> UserAuthenticators => Set<UserAuthenticator>();
    public DbSet<FaceTemplate> FaceTemplates => Set<FaceTemplate>();
    public DbSet<VoiceTemplate> VoiceTemplates => Set<VoiceTemplate>();
    public DbSet<AuthAttempt> AuthAttempts => Set<AuthAttempt>();
    public DbSet<RecoveryCode> RecoveryCodes => Set<RecoveryCode>();
    public DbSet<TotpSecret> TotpSecrets => Set<TotpSecret>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuthDbContext).Assembly);
    }
}