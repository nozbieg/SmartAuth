using SmartAuth.Api.Features.Auth;

namespace SmartAuth.Api.Endpoints;

public static class AuthEndpoints
{
    public static WebApplication UseAuthEndpoints(this WebApplication app)
    {
        var auth = app.MapGroup("/api/auth");
        auth.WithTags("Auth")
            .AddEndpointFilterFactory((context, next) => async invocationContext => await next(invocationContext));

        auth.MapPost("/register", (AuthRegisterCommand req) => req);
        auth.MapPost("/login", (AuthLoginCommand req) => req);
        auth.MapPost("/2fa/code/verify", (TwoFaCodeVerifyCommand req) => req)
            .RequireAuthorization();

        return app;
    }
}