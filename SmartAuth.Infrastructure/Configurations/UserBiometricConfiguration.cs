using SmartAuth.Domain.Entities;

namespace SmartAuth.Infrastructure.Configurations;

public sealed class UserBiometricConfiguration : IEntityTypeConfiguration<UserBiometric>
{
    public void Configure(EntityTypeBuilder<UserBiometric> b)
    {
        b.ToTable("user_biometrics");

        b.HasOne(x => x.User)
            .WithMany(u => u.Biometrics)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        b.HasIndex(x => new { x.UserId, x.Kind, x.IsActive });

        b.Property(x => x.Kind)
            .HasConversion<int>()
            .IsRequired();

        b.Property(x => x.Version)
            .HasMaxLength(64)
            .IsRequired();

        b.Property(x => x.Embedding)
            .HasColumnType("vector(512)")
            .IsRequired();

        b.Property(x => x.QualityScore)
            .HasPrecision(6,3);

        b.Property(x => x.LivenessMethod)
            .HasConversion<int>()
            .IsRequired();

        b.Property(x => x.IsActive)
            .IsRequired();

        b.Property(x => x.AudioSampleRate);

        b.Property(x => x.AudioDurationSeconds)
            .HasPrecision(8, 3);
    }
}
