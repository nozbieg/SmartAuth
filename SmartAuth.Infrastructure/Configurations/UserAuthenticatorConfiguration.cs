using SmartAuth.Domain.Entities;

namespace SmartAuth.Infrastructure.Configurations;

public sealed class UserAuthenticatorConfiguration : IEntityTypeConfiguration<UserAuthenticator>
{
    public void Configure(EntityTypeBuilder<UserAuthenticator> b)
    {
        b.ToTable("user_authenticators");

        b.HasIndex(a => new { a.UserId, a.Type })
            .IsUnique();

        b.Property(a => a.Type)
            .HasConversion<int>()
            .IsRequired();

        b.Property(a => a.Secret)
            .IsRequired()
            .HasMaxLength(128);

        b.Property(a => a.LastUsedAt)
            .HasColumnType("timestamp with time zone")
            .HasPrecision(3);

        b.Property(a => a.IsActive)
            .IsRequired();
    }
}
