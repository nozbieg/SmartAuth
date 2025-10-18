using SmartAuth.Domain.Common;

namespace SmartAuth.Domain.Entities;

public class User : AuditableEntity
{
    public string Email { get; set; } = string.Empty;
    public byte[] PasswordHash { get; set; } = [];
    public byte[] PasswordSalt { get; set; } = [];
    public UserStatus Status { get; set; } = UserStatus.Active;
    public DateTimeOffset? LastLoginAt { get; set; }
    public ICollection<UserAuthenticator> Authenticators { get; set; } = new List<UserAuthenticator>();
    public UserLoginConfiguration? LoginConfiguration { get; set; }
}