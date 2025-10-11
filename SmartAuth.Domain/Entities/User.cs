namespace SmartAuth.Domain.Entities;

public class User {
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public byte[] PasswordHash { get; set; } = Array.Empty<byte>();
    public byte[] PasswordSalt { get; set; } = Array.Empty<byte>();
    public UserStatus Status { get; set; } = UserStatus.Active;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastLoginAt { get; set; }
    public ICollection<UserAuthenticator> Authenticators { get; set; } = new List<UserAuthenticator>();
}