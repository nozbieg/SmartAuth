using System.ComponentModel.DataAnnotations;

namespace SmartAuth.Api.Contracts;

public sealed class CreateUserDto
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, MinLength(8)] public string Password { get; set; } = string.Empty;
}
public sealed record UserCreatedDto(Guid Id, string Email);