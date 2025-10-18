using SmartAuth.Api.Contracts;
using SmartAuth.Api.Utilities;

namespace SmartAuth.Api.Features.Auth;

public record AuthLoginCommand(string Email, string Password) : IRequest<CommandResult<AuthLoginResult>>;

public record AuthLoginResult(bool Requires2Fa, string? Token = null, List<string>? Methods = null);

public class AuthLoginValidator : Validator<AuthLoginCommand>
{
    protected override Task ValidateParams(AuthLoginCommand request)
    {
        if (string.IsNullOrWhiteSpace(request.Email)) Metadata.Add(nameof(request.Email), "Email is required");
        if (string.IsNullOrWhiteSpace(request.Password)) Metadata.Add(nameof(request.Password), "Password is required");
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
            .FirstOrDefaultAsync(u => u.Email.Equals(emailNorm, StringComparison.CurrentCultureIgnoreCase),
                cancellationToken: ct);

        if (user is null)
            return CommandResult<AuthLoginResult>.Fail(Errors.NotFound(nameof(Domain.Entities.User), emailNorm));
        if (user.Status != UserStatus.Active)
            return CommandResult<AuthLoginResult>.Fail(Errors.Forbidden(nameof(Domain.Entities.User)));

        var ok = AuthCrypto.VerifyPassword(req.Password, user.PasswordHash, user.PasswordSalt);
        if (!ok) return CommandResult<AuthLoginResult>.Fail(Errors.InvalidCredentials());

        var flags = cfg.GetSection("FeatureFlags").Get<FeatureFlags>()!;
        var methods = new List<string>();
        if (flags.twofa_code) methods.Add("code");

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