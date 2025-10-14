using System.ComponentModel.DataAnnotations;

namespace SmartAuth.Api.Contracts;

public sealed class FaceEnrollCompleteDto
{
    [Required] public Guid AuthenticatorId { get; set; }

    [Required, MinLength(8)] public float[] Embedding { get; set; } = [];

    public float QualityScore { get; set; } = 0f;
    public string? ModelVersion { get; set; } = "face-v1";
}