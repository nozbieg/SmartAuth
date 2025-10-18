using SmartAuth.Domain.Common;

namespace SmartAuth.Infrastructure.Configurations;

public sealed class AuditableEntityConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
    where TEntity : AuditableEntity
{
    public void Configure(EntityTypeBuilder<TEntity> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.Id)
            .ValueGeneratedNever();

        b.Property(e => e.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("now() at time zone 'utc'")
            .IsRequired();

        b.Property(e => e.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasDefaultValueSql("now() at time zone 'utc'")
            .IsRequired();
    }
}

public static class ModelBuilderAuditableExtensions
{
    public static void ApplyAuditableConfigurations(ModelBuilder modelBuilder)
    {
        var auditableBase = typeof(AuditableEntity);

        var auditableEntityClrTypes = modelBuilder.Model.GetEntityTypes()
            .Select(t => t.ClrType)
            .Where(t => auditableBase.IsAssignableFrom(t))
            .Distinct()
            .ToList();

        foreach (var clrType in auditableEntityClrTypes)
        {
            var cfgType = typeof(AuditableEntityConfiguration<>).MakeGenericType(clrType);
            var cfgInstance = Activator.CreateInstance(cfgType);

            var applyConfigMethod = typeof(ModelBuilder)
                .GetMethods()
                .First(m =>
                    m.Name == nameof(ModelBuilder.ApplyConfiguration) &&
                    m.GetParameters().Length == 1 &&
                    m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() ==
                    typeof(IEntityTypeConfiguration<>));

            applyConfigMethod
                .MakeGenericMethod(clrType)
                .Invoke(modelBuilder, new[] { cfgInstance });
        }
    }
}