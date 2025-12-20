using SmartAuth.Api.Contracts;
using SmartAuth.Api.Utilities;

namespace SmartAuth.Api.Features.Auth;

public record TwoFaCodeVerifyCommand(string Code) : IRequest<CommandResult<TwoFaCodeVerifyResult>>;

public record TwoFaCodeVerifyResult(string Jwt);

public class TwoFaCodeVerifyValidator : Validator<TwoFaCodeVerifyCommand>
{
    protected override Task ValidateParams(TwoFaCodeVerifyCommand request)
    {
        if (string.IsNullOrEmpty(request.Code)) Metadata.Add(nameof(request.Code), "Kod jest wymagany");
        return Task.CompletedTask;
    }
}

public class
    TwoFaCodeVerifyCommandHandler(IHttpContextAccessor accessor, IConfiguration cfg)
    : IRequestHandler<TwoFaCodeVerifyCommand, CommandResult<TwoFaCodeVerifyResult>>
{
    public async Task<CommandResult<TwoFaCodeVerifyResult>> Handle(TwoFaCodeVerifyCommand req,
        CancellationToken cancellationToken)
    {
        var ctx = accessor.HttpContext;
        if (ctx is null)
            return CommandResult<TwoFaCodeVerifyResult>.Fail(Errors.Internal("Brak kontekstu HTTP"));
        var email = TokenUtilities.GetSubjectFromToken(ctx);
        if (email is null) return CommandResult<TwoFaCodeVerifyResult>.Fail(Errors.Unauthorized());


        var db = ctx.RequestServices.GetRequiredService<AuthDbContext>();
        var cfgFlags = ctx.RequestServices.GetRequiredService<IConfiguration>().GetSection("FeatureFlags")
            .Get<FeatureFlags>()!;
        var user = await db.Users.Include(u => u.Authenticators)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        if (user is null)
            return CommandResult<TwoFaCodeVerifyResult>.Fail(Errors.NotFound(nameof(User), email));

        var hasTotp = user.Authenticators.Any(a => a.Type == AuthenticatorType.Totp && a.IsActive);
        var totpAuth = user.Authenticators.FirstOrDefault(a => a.Type == AuthenticatorType.Totp && a.IsActive);
        var totpOk = false;
        if (totpAuth is not null)
        {
            totpOk = SmartAuth.Infrastructure.Security.Totp.ValidateCode(totpAuth.Secret, req.Code);
            if (totpOk)
            {
                totpAuth.LastUsedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        var codeOk = !hasTotp && cfgFlags.twofa_code;
        if (!totpOk && !codeOk)
            return CommandResult<TwoFaCodeVerifyResult>.Fail(Errors.InvalidCredentials());


        var jwt = TokenUtilities.IssueAccessToken(cfg, email, email);
        return CommandResult<TwoFaCodeVerifyResult>.Ok(new TwoFaCodeVerifyResult(jwt));
    }
}