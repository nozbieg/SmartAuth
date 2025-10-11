using System.ComponentModel.DataAnnotations;
using SmartAuth.Domain.Entities;

namespace SmartAuth.Api.Contracts;

// ======= Authenticators =======
public sealed class AddAuthenticatorDto
{
    [Required] public Guid UserId { get; set; }
    [Required] public AuthenticatorType Type { get; set; }
    [Required, MaxLength(100)] public string DisplayName { get; set; } = string.Empty;
}
public sealed record AuthenticatorAddedDto(Guid Id, AuthenticatorType Type, string DisplayName);