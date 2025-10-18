using System.Net;
using SmartAuth.Infrastructure.Tracing;

namespace SmartAuth.Infrastructure.Commons;

public sealed class Error
{
    public string Code { get; init; } = default!;

    public string Message { get; init; } = default!;

    public string? Detail { get; init; }

    public HttpStatusCode Status { get; init; }

    public IDictionary<string, object>? Metadata { get; init; }

    public string TraceId { get; init; } = OtelTrace.CurrentTraceId();


    public override string ToString() => $"{Code}: {Message}";
}

public static class Errors
{
    public static Error Validation(string message, Dictionary<string, object>? metadata = null) => new()
    {
        Code = "validation.error",
        Message = message,
        Status = HttpStatusCode.UnprocessableEntity,
        Metadata = metadata ?? new Dictionary<string, object>(),
    };

    public static Error NotFound(string entity, string? id = null) => new()
    {
        Code = "common.not_found",
        Message = id is null ? $"{entity} not found." : $"{entity} '{id}' not found.",
        Status = HttpStatusCode.NotFound,
        Metadata = new Dictionary<string, object>
        {
            ["entity"] = entity,
            ["id"] = id ?? ""
        },
    };

    public static Error Unauthorized() => new()
    {
        Code = "auth.unauthorized",
        Message = "You are not authorized to perform this action.",
        Status = HttpStatusCode.Unauthorized,
    };

    public static Error Forbidden(string? reason = null) => new()
    {
        Code = "auth.forbidden",
        Message = reason ?? "Access to this resource is forbidden.",
        Status = HttpStatusCode.Forbidden,
    };

    public static Error Conflict(string entity, string? reason = null) => new()
    {
        Code = "common.conflict",
        Message = reason ?? $"{entity} already exists.",
        Status = HttpStatusCode.Conflict,
    };

    public static Error InvalidCredentials() => new()
    {
        Code = "auth.invalid_credentials",
        Message = "The provided credentials are incorrect.",
        Status = HttpStatusCode.BadRequest,
    };

    public static Error Internal(string? detail = null) => new()
    {
        Code = "system.internal_error",
        Message = "An unexpected error occurred. Please try again later.",
        Detail = detail,
        Status = HttpStatusCode.InternalServerError,
        Metadata = new Dictionary<string, object>(),
    };
}