using System.ComponentModel.DataAnnotations;

namespace SmartAuth.Api.Contracts;

public sealed class FaceVerifyDto
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;

    [Required, MinLength(8)] public float[] Embedding { get; set; } = [];

    public double RequiredScore { get; set; } = 0.85;
    public string? Ip { get; set; }
    public string? UserAgent { get; set; }
}