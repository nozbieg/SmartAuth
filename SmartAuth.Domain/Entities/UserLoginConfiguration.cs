using SmartAuth.Domain.Common;

namespace SmartAuth.Domain.Entities;

public sealed class UserLoginConfiguration : AuditableEntity
{
    public Guid UserId { get; set; } // FK do User
    public User User { get; set; } = default!; 
    
    // --- TOTP ---
    public bool TotpEnabled { get; set; }

    public byte[]? TotpSecretCiphertext { get; set; }

    public byte[]? TotpSecretNonce { get; set; }

    public string? TotpKeyId { get; set; }

    public string TotpAlgorithm { get; set; } = "SHA1";

    public int TotpDigits { get; set; } = 6;

    public int TotpPeriodSeconds { get; set; } = 30;

    public int TotpDriftSteps { get; set; } = 1;

    public string? TotpLastCodeHash { get; set; }

    public DateTime? TotpLastVerifiedAtUtc { get; set; }

    public bool FaceEnabled { get; set; }
    public string? FaceProvider { get; set; }
    public DateTime? FaceUpdatedAtUtc { get; set; }

    public bool VoiceEnabled { get; set; }
    public string? VoiceProvider { get; set; }
    public DateTime? VoiceUpdatedAtUtc { get; set; }
}