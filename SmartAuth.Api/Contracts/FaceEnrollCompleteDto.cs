using System.ComponentModel.DataAnnotations;

namespace SmartAuth.Api.Contracts;

// ======= Face Enrollment =======
public sealed class FaceEnrollCompleteDto
{
    [Required] public Guid AuthenticatorId { get; set; }

    // JSON: [0.012, 0.98, ...]
    [Required, MinLength(8)] public float[] Embedding { get; set; } = [];

    public float QualityScore { get; set; } = 0f;
    public string? ModelVersion { get; set; } = "face-v1";
}