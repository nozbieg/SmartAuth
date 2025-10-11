namespace SmartAuth.Domain.Entities;

public class RecoveryCode
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;
    public string CodeHash { get; set; } = string.Empty;
    public DateTimeOffset? UsedAt { get; set; }
}
