namespace SmartAuth.Domain.Entities;

public class TotpSecret
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "AuthSystem";
    public bool Enforced { get; set; } = false;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}