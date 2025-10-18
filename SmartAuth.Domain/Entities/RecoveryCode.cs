using SmartAuth.Domain.Common;

namespace SmartAuth.Domain.Entities;

public class RecoveryCode : AuditableEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;
    public string CodeHash { get; set; } = string.Empty;
    public DateTimeOffset? UsedAt { get; set; }
}
