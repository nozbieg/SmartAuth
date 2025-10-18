using SmartAuth.Domain.Common;

namespace SmartAuth.Domain.Entities;

public class TotpSecret : AuditableEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "AuthSystem";
    public bool Enforced { get; set; } = false;
}