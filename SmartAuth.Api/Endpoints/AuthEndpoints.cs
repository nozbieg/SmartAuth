using SmartAuth.Api.Contracts;
using SmartAuth.Api.Features.Auth;
using SmartAuth.Api.Utilities;

namespace SmartAuth.Api.Endpoints;

public static class AuthEndpoints
{
    public static WebApplication Use2FaEndpoints(this WebApplication app)
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