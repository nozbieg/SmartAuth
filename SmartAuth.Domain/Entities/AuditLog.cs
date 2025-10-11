namespace SmartAuth.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}