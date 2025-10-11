namespace SmartAuth.Api.Contracts;

public record RegisterRequest(string Email, string Password, string? DisplayName);