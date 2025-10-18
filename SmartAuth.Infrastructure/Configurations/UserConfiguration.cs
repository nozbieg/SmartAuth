namespace SmartAuth.Infrastructure.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");

        b.HasIndex(u => u.Email)
            .IsUnique();

        b.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

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
    }
}