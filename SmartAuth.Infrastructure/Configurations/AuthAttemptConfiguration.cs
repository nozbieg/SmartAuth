namespace SmartAuth.Infrastructure.Configurations;

public sealed class AuthAttemptConfiguration : IEntityTypeConfiguration<AuthAttempt>
{
    public void Configure(EntityTypeBuilder<AuthAttempt> e)
    {
        e.ToTable("auth_attempts");
        e.Property(x => x.Type).HasConversion<int>().IsRequired();
        e.Property(x => x.Success).IsRequired();
    }
}