using SmartAuth.Domain.Interfaces;

namespace SmartAuth.Domain.Common;

public abstract class AuditableEntity : IAuditEntity, IEntityId
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}