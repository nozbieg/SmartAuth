
namespace SmartAuth.Infrastructure.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> e)
    {
        e.ToTable("users");
        e.HasKey(x => x.Id);
        e.Property(x => x.Email).HasMaxLength(320).IsRequired();
        e.HasIndex(x => x.Email).IsUnique();
        e.Property(x => x.Status).HasConversion<int>();
        e.Property(x => x.PasswordHash).IsRequired();
        e.Property(x => x.PasswordSalt).IsRequired();
        e.Property(x => x.CreatedAt).IsRequired();
    }
}