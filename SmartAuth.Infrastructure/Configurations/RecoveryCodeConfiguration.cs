namespace SmartAuth.Infrastructure.Configurations;

public sealed class RecoveryCodeConfiguration : IEntityTypeConfiguration<RecoveryCode>
{
    public void Configure(EntityTypeBuilder<RecoveryCode> e)
    {
        e.ToTable("recovery_codes");
        e.HasKey(x => x.Id);
        e.Property(x => x.CodeHash).HasColumnName("code_hash").HasMaxLength(256).IsRequired();
        e.Property(x => x.UsedAt).HasColumnName("used_at");
        e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
    }
}