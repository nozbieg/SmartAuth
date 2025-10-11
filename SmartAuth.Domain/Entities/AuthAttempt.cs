namespace SmartAuth.Domain.Entities;

public class AuthAttempt {
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public AttemptType Type { get; set; }
    public bool Success { get; set; }
    public double? Score { get; set; }      // podobieństwo / odległość
    public double? RiskScore { get; set; }  // do integracji z Risk Engine
    public string Ip { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public Guid? DeviceId { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}