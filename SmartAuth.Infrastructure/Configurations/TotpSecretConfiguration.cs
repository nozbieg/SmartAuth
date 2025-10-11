namespace SmartAuth.Infrastructure.Configurations;

public sealed class TotpSecretConfiguration : IEntityTypeConfiguration<TotpSecret>
{
    public void Configure(EntityTypeBuilder<TotpSecret> e)
    {
        e.ToTable("totp_secrets");
        e.HasKey(x => x.Id);
        e.Property(x => x.Secret).HasMaxLength(200).IsRequired();
        e.Property(x => x.Issuer).HasMaxLength(80).IsRequired();
        e.Property(x => x.Enforced);
        e.Property(x => x.CreatedAt).HasColumnName("created_at");
        e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
    }
}