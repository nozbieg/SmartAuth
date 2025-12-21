using SmartAuth.Api.Contracts;
using SmartAuth.Api.Utilities;

namespace SmartAuth.Api.Features.Auth;

public record AuthLoginCommand(string Email, string Password) : IRequest<CommandResult<AuthLoginResult>>;

public record AuthLoginResult(bool Requires2Fa, string? Token = null, List<string>? Methods = null);

public class AuthLoginValidator : Validator<AuthLoginCommand>
{
    protected override Task ValidateParams(AuthLoginCommand request)
    {
        if (string.IsNullOrWhiteSpace(request.Email)) Metadata.Add(nameof(request.Email), Messages.Validation.EmailRequired);
        if (string.IsNullOrWhiteSpace(request.Password)) Metadata.Add(nameof(request.Password), Messages.Validation.PasswordRequired);
        return Task.CompletedTask;
    }
}

public class AuthLoginCommandHandler(AuthDbContext db, IConfiguration cfg)
    : IRequestHandler<AuthLoginCommand, CommandResult<AuthLoginResult>>
{
    public async Task<CommandResult<AuthLoginResult>> Handle(AuthLoginCommand req, CancellationToken ct)
    {
        var emailNorm = req.Email.Trim().ToLowerInvariant();
        var user = await db.Users
            .Include(u => u.Authenticators)
            .Include(u => u.Biometrics)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == emailNorm, ct);

        if (user is null)
            return CommandResult<AuthLoginResult>.Fail(Errors.NotFound(nameof(User), emailNorm));
        if (user.Status != UserStatus.Active)
            return CommandResult<AuthLoginResult>.Fail(Errors.Forbidden(nameof(User)));

        var ok = AuthCrypto.VerifyPassword(req.Password, user.PasswordHash, user.PasswordSalt);
        if (!ok) return CommandResult<AuthLoginResult>.Fail(Errors.InvalidCredentials());

        var flags = cfg.GetSection("FeatureFlags").Get<FeatureFlags>()!;
        var methods = new List<string>();
        
        var hasActiveTotp = user.Authenticators.Any(a => a.Type == AuthenticatorType.Totp && a.IsActive);
        if (hasActiveTotp)
        {
            methods.Add("totp");
        }
        else if (flags.twofa_code)
        {
            methods.Add("code");
        }
        
        if (flags.twofa_face && user.Biometrics.Any(b => b.Kind == AuthenticatorType.Face && b.IsActive))
            methods.Add("face");

        if (flags.twofa_voice && user.Biometrics.Any(b => b.Kind == AuthenticatorType.Voice && b.IsActive))
            methods.Add("voice");

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        if (methods.Count == 0)
        {
            var jwt = TokenUtilities.IssueAccessToken(cfg, user.Email, user.Email);
            return CommandResult<AuthLoginResult>.Ok(new AuthLoginResult(false, Token: jwt));
        }

        var temp = TokenUtilities.IssueTempToken(cfg, user.Email);
        return CommandResult<AuthLoginResult>.Ok(new AuthLoginResult(true, temp, methods));
    }
}