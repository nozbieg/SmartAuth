using SmartAuth.Domain.Common;

namespace SmartAuth.Domain.Entities;

public class UserAuthenticator : AuditableEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public AuthenticatorType Type { get; set; } = AuthenticatorType.Totp;
    public string Secret { get; set; } = string.Empty;
    public DateTimeOffset? LastUsedAt { get; set; }
    public bool IsActive { get; set; } = true;
}

public enum AuthenticatorType { Totp = 1, Face = 2, Voice = 3 }
