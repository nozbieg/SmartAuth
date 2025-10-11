namespace SmartAuth.Domain.Entities;

public class UserAuthenticator {
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;
    public AuthenticatorType Type { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public bool IsEnrolled { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public FaceTemplate? FaceTemplate { get; set; }
    public VoiceTemplate? VoiceTemplate { get; set; }
}