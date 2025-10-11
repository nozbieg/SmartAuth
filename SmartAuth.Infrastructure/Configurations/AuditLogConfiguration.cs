namespace SmartAuth.Infrastructure.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> e)
    {
        e.ToTable("audit_logs");
        e.HasKey(x => x.Id);
        e.Property(x => x.Action).HasMaxLength(120).IsRequired();
        e.Property(x => x.Details).HasColumnType("jsonb").HasDefaultValue("{}");
        e.Property(x => x.CreatedAt).HasColumnName("created_at");
    }
}