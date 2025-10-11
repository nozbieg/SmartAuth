namespace SmartAuth.Infrastructure.Configurations;

public sealed class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> e)
    {
        e.ToTable("sessions");
        e.HasKey(x => x.Id);
        e.Property(x => x.RefreshTokenHash).HasColumnName("refresh_token_hash").HasMaxLength(256).IsRequired();
        e.Property(x => x.CreatedAt).HasColumnName("created_at");
        e.Property(x => x.ExpiresAt).HasColumnName("expires_at");
        e.Property(x => x.RevokedAt).HasColumnName("revoked_at");
        e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
    }
}