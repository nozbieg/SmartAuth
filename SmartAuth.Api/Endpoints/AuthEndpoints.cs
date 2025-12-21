using SmartAuth.Api.Endpoints.Filters;
using SmartAuth.Api.Features.Auth;

namespace SmartAuth.Api.Endpoints;

public static class AuthEndpoints
{
    public static WebApplication UseAuthEndpoints(this WebApplication app)
    {
        var auth = app.MapGroup("/api/auth");
        auth.WithTags("Auth")
            .AddEndpointFilter<MediatorEndpointFilter>();

        auth.MapPost("/register", (AuthRegisterCommand req) => req);
        auth.MapPost("/login", (AuthLoginCommand req) => req);
        auth.MapPost("/2fa/code/verify", (TwoFaCodeVerifyCommand req) => req).RequireAuthorization();
        auth.MapPost("/2fa/totp/setup", (TwoFaTotpSetupCommand req) => req).RequireAuthorization();
        auth.MapPost("/2fa/totp/enable", (TwoFaTotpEnableCommand req) => req).RequireAuthorization();
        auth.MapPost("/2fa/totp/disable", (TwoFaTotpDisableCommand req) => req).RequireAuthorization();
        auth.MapGet("/2fa/totp/status", () => new TwoFaTotpStatusQuery()).RequireAuthorization();
        auth.MapPost("/2fa/face/enroll", (TwoFaFaceEnrollCommand req) => req).RequireAuthorization();
        auth.MapPost("/2fa/face/verify", (TwoFaFaceVerifyCommand req) => req).RequireAuthorization();
        auth.MapDelete("/2fa/face", () => new TwoFaFaceDisableCommand()).RequireAuthorization();
        auth.MapGet("/2fa/face/status", () => new TwoFaFaceStatusQuery()).RequireAuthorization();

        auth.MapPost("/2fa/voice/enroll", (TwoFaVoiceEnrollCommand req) => req).RequireAuthorization();
        auth.MapPost("/2fa/voice/verify", (TwoFaVoiceVerifyCommand req) => req).RequireAuthorization();
        auth.MapDelete("/2fa/voice", () => new TwoFaVoiceDisableCommand()).RequireAuthorization();
        auth.MapGet("/2fa/voice/status", () => new TwoFaVoiceStatusQuery()).RequireAuthorization();

        return app;
    }
}