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

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasPostgresExtension("vector");

        b.Entity<User>(e =>
        {
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Status).HasConversion<int>();
        });

        b.Entity<UserAuthenticator>(e =>
        {
            e.Property(x => x.Type).HasConversion<int>();
            e.HasOne(x => x.User).WithMany(u => u.Authenticators).HasForeignKey(x => x.UserId);
            e.HasOne(x => x.FaceTemplate).WithOne(t => t.Authenticator)
                .HasForeignKey<FaceTemplate>(t => t.AuthenticatorId);
            e.HasOne(x => x.VoiceTemplate).WithOne(t => t.Authenticator)
                .HasForeignKey<VoiceTemplate>(t => t.AuthenticatorId);
        });

        b.Entity<FaceTemplate>(e =>
        {
            e.Property(x => x.Embedding)
                .HasColumnType("vector(512)");
        });

        b.Entity<VoiceTemplate>(e =>
        {
            e.Property(x => x.Embedding)
                .HasColumnType("vector(256)");
        });

        b.Entity<AuthAttempt>(e =>
        {
            e.Property(x => x.Type).HasConversion<int>();
            e.HasIndex(x => x.CreatedAt);
        });

    }
}