using SmartAuth.Domain.Common;

namespace SmartAuth.Domain.Entities;

public class UserAuthenticator : AuditableEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;
    public AuthenticatorType Type { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public bool IsEnrolled { get; set; }
    public FaceTemplate? FaceTemplate { get; set; }
    public VoiceTemplate? VoiceTemplate { get; set; }
}