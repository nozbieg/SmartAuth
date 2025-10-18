using SmartAuth.Domain.Common;

namespace SmartAuth.Domain.Entities;

public class Device : AuditableEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;
    public string Name { get; set; } = string.Empty;
    public string? PublicKey { get; set; }
    public bool Trusted { get; set; } = false;
    public DateTimeOffset RegisteredAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastUsedAt { get; set; }
}