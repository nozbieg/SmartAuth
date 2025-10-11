using System.ComponentModel.DataAnnotations;

namespace SmartAuth.Api.Contracts;

public sealed class VoiceVerifyDto
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;

    [Required, MinLength(8)] public float[] Embedding { get; set; } = [];

    public double RequiredScore { get; set; } = 0.80;
    public string? Ip { get; set; }
    public string? UserAgent { get; set; }
}