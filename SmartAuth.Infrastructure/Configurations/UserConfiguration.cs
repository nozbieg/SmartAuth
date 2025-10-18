namespace SmartAuth.Infrastructure.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> e)
    {
        e.ToTable("users");
        e.Property(x => x.Email).HasMaxLength(320).IsRequired();
        e.HasIndex(x => x.Email).IsUnique();
        e.Property(x => x.Status).HasConversion<int>();
        e.Property(x => x.PasswordHash).IsRequired();
        e.Property(x => x.PasswordSalt).IsRequired();

        e.HasOne(u => u.LoginConfiguration)
            .WithOne(c => c.User)
            .HasForeignKey<UserLoginConfiguration>(c => c.UserId)
            .HasConstraintName("fk_user_login_configurations_users_user_id")
            .OnDelete(DeleteBehavior.Cascade);
    }
}