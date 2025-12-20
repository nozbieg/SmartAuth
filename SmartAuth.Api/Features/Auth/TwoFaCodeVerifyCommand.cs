using SmartAuth.Api.Contracts;
using SmartAuth.Api.Utilities;

namespace SmartAuth.Api.Features.Auth;

public record TwoFaCodeVerifyCommand(string Code) : IRequest<CommandResult<TwoFaCodeVerifyResult>>;

public record TwoFaCodeVerifyResult(string Jwt);

public class TwoFaCodeVerifyValidator : Validator<TwoFaCodeVerifyCommand>
{
    protected override Task ValidateParams(TwoFaCodeVerifyCommand request)
    {
        if (string.IsNullOrEmpty(request.Code)) Metadata.Add(nameof(request.Code), Messages.Validation.CodeRequired);
        return Task.CompletedTask;
    }
}

public class TwoFaCodeVerifyCommandHandler(IHttpContextAccessor accessor, IConfiguration cfg)
    : IRequestHandler<TwoFaCodeVerifyCommand, CommandResult<TwoFaCodeVerifyResult>>
{
    public async Task<CommandResult<TwoFaCodeVerifyResult>> Handle(TwoFaCodeVerifyCommand req, CancellationToken ct)
    {
        var (ctx, email, authError) = HandlerHelpers.GetAuthenticatedContext(accessor);
        if (authError is not null)
            return CommandResult<TwoFaCodeVerifyResult>.Fail(authError);

        var db = ctx!.RequestServices.GetRequiredService<AuthDbContext>();
        var cfgFlags = cfg.GetSection("FeatureFlags").Get<FeatureFlags>()!;
        
        var (user, userError) = await HandlerHelpers.GetUserWithAuthenticatorsAsync(db, email!, ct);
        if (userError is not null)
            return CommandResult<TwoFaCodeVerifyResult>.Fail(userError);

        var hasTotp = user!.Authenticators.Any(a => a.Type == AuthenticatorType.Totp && a.IsActive);
        var totpAuth = user.Authenticators.FirstOrDefault(a => a.Type == AuthenticatorType.Totp && a.IsActive);
        var totpOk = false;
        
        if (totpAuth is not null)
        {
            totpOk = Totp.ValidateCode(totpAuth.Secret, req.Code);
            if (totpOk)
            {
                totpAuth.LastUsedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(ct);
            }
        }

        var codeOk = !hasTotp && cfgFlags.twofa_code;
        if (!totpOk && !codeOk)
            return CommandResult<TwoFaCodeVerifyResult>.Fail(Errors.InvalidCredentials());

        var jwt = TokenUtilities.IssueAccessToken(cfg, email!, email!);
        return CommandResult<TwoFaCodeVerifyResult>.Ok(new TwoFaCodeVerifyResult(jwt));
    }
}