using System.ComponentModel.DataAnnotations;

namespace SmartAuth.Api.Contracts;

public sealed class VerifyTotpDto
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, MaxLength(12)] public string Code { get; set; } = string.Empty;
}