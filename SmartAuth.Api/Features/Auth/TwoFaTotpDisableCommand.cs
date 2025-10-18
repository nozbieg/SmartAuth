using SmartAuth.Api.Utilities;

namespace SmartAuth.Api.Features.Auth;

public record TwoFaTotpDisableCommand() : IRequest<CommandResult<TwoFaTotpDisableResult>>;
public record TwoFaTotpDisableResult(string Message = "TOTP disabled");

public class TwoFaTotpDisableValidator : Validator<TwoFaTotpDisableCommand>
{
    protected override Task ValidateParams(TwoFaTotpDisableCommand request) => Task.CompletedTask;
}

public class TwoFaTotpDisableCommandHandler(AuthDbContext db, IHttpContextAccessor accessor)
    : IRequestHandler<TwoFaTotpDisableCommand, CommandResult<TwoFaTotpDisableResult>>
{
    public async Task<CommandResult<TwoFaTotpDisableResult>> Handle(TwoFaTotpDisableCommand req, CancellationToken ct)
    {
        var ctx = accessor.HttpContext;
        if (ctx is null) return CommandResult<TwoFaTotpDisableResult>.Fail(Errors.Internal("Missing HttpContext"));
        var email = TokenUtilities.GetSubjectFromToken(ctx);
        if (email is null) return CommandResult<TwoFaTotpDisableResult>.Fail(Errors.Unauthorized());

        var user = await db.Users.Include(u => u.Authenticators).FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null) return CommandResult<TwoFaTotpDisableResult>.Fail(Errors.NotFound("User", email));

        var active = user.Authenticators.FirstOrDefault(a => a.Type == AuthenticatorType.Totp && a.IsActive);
        if (active is null) return CommandResult<TwoFaTotpDisableResult>.Fail(Errors.NotFound("ActiveTotp", email));

        db.UserAuthenticators.Remove(active);
        await db.SaveChangesAsync(ct);
        return CommandResult<TwoFaTotpDisableResult>.Ok(new TwoFaTotpDisableResult());
    }
}

