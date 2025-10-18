namespace SmartAuth.Infrastructure.Configurations;

public sealed class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> e)
    {
        e.ToTable("devices");
        e.Property(x => x.Name).HasMaxLength(120).IsRequired();
        e.Property(x => x.PublicKey).HasColumnName("public_key");
        e.Property(x => x.Trusted);
        e.Property(x => x.RegisteredAt).HasColumnName("registered_at");
        e.Property(x => x.LastUsedAt).HasColumnName("last_used_at");
        e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
    }
}