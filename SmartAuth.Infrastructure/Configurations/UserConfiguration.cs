namespace SmartAuth.Infrastructure.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users", t =>
        {
            t.HasCheckConstraint("ck_users_email_lowercase", "email = lower(email)");
        });

        b.HasIndex(u => u.Email)
            .IsUnique();

        b.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256)
            .HasConversion(v => v.Trim().ToLowerInvariant(), v => v);

        b.Property(u => u.PasswordHash)
            .IsRequired();

        b.Property(u => u.PasswordSalt)
            .IsRequired();

        b.Property(u => u.Status)
            .HasConversion<int>() 
            .IsRequired()
            .HasMaxLength(32);

        b.Property(u => u.LastLoginAt)
            .HasColumnType("timestamp with time zone")
            .HasPrecision(3);

        b.HasMany(u => u.Authenticators)
            .WithOne(a => a.User!)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}