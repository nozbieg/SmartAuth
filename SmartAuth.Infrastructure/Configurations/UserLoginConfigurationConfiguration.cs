namespace SmartAuth.Infrastructure.Configurations;

public sealed class UserLoginConfigurationConfiguration : IEntityTypeConfiguration<UserLoginConfiguration>
{
    public void Configure(EntityTypeBuilder<UserLoginConfiguration> b)
    {
        b.ToTable("user_login_configurations");

        b.Property(x => x.UserId)
            .IsRequired();

        b.HasIndex(x => x.UserId)
            .IsUnique();

        b.HasOne(x => x.User)
            .WithOne(u => u.LoginConfiguration)
            .HasForeignKey<UserLoginConfiguration>(x => x.UserId)
            .HasConstraintName("fk_user_login_configurations_users_user_id")
            .OnDelete(DeleteBehavior.Cascade);

        b.Property(x => x.TotpEnabled)
            .IsRequired();

        b.Property(x => x.TotpKeyId)
            .HasMaxLength(200);

        b.Property(x => x.TotpAlgorithm)
            .HasMaxLength(10)
            .HasDefaultValue("SHA1");

        b.Property(x => x.TotpDigits)
            .HasDefaultValue(6);

        b.Property(x => x.TotpPeriodSeconds)
            .HasDefaultValue(30);

        b.Property(x => x.TotpDriftSteps)
            .HasDefaultValue(1);

        b.Property(x => x.TotpLastCodeHash)
            .HasMaxLength(200);

        b.Property(x => x.FaceEnabled)
            .IsRequired();

        b.Property(x => x.FaceProvider)
            .HasMaxLength(100);

        b.Property(x => x.VoiceEnabled)
            .IsRequired();

        b.Property(x => x.VoiceProvider)
            .HasMaxLength(100);
    }
}