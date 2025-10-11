using System.ComponentModel.DataAnnotations;

namespace SmartAuth.Api.Contracts;

// ======= Voice Enrollment =======
public sealed class VoiceEnrollCompleteDto
{
    [Required] public Guid AuthenticatorId { get; set; }

    [Required, MinLength(8)] public float[] Embedding { get; set; } = [];

    [MaxLength(120)] public string? Phrase { get; set; }
    public int SampleRate { get; set; } = 16000;
    public string? ModelVersion { get; set; } = "voice-v1";
}