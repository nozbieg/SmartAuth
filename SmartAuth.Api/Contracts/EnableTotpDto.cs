using System.ComponentModel.DataAnnotations;

namespace SmartAuth.Api.Contracts;

public sealed class EnableTotpDto
{
    [Required] public Guid UserId { get; set; }
    [Required, MaxLength(80)] public string Issuer { get; set; } = "AuthSystem";
}