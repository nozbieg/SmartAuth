using SmartAuth.Domain.Common;

namespace SmartAuth.Domain.Entities;

public class Session : AuditableEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;
    public string RefreshTokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}