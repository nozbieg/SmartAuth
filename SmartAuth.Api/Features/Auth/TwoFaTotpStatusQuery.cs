using SmartAuth.Api.Utilities;

namespace SmartAuth.Api.Features.Auth;

public record TwoFaTotpStatusQuery() : IRequest<CommandResult<TwoFaTotpStatusResult>>;
public record TwoFaTotpStatusResult(bool Active);

public class TwoFaTotpStatusQueryHandler(AuthDbContext db, IHttpContextAccessor accessor)
    : IRequestHandler<TwoFaTotpStatusQuery, CommandResult<TwoFaTotpStatusResult>>
{
    public async Task<CommandResult<TwoFaTotpStatusResult>> Handle(TwoFaTotpStatusQuery req, CancellationToken ct)
    {
        var ctx = accessor.HttpContext;
        if (ctx is null) return CommandResult<TwoFaTotpStatusResult>.Fail(Errors.Internal("Brak kontekstu HTTP"));
        var email = TokenUtilities.GetSubjectFromToken(ctx);
        if (email is null) return CommandResult<TwoFaTotpStatusResult>.Fail(Errors.Unauthorized());

        var active = await db.UserAuthenticators.AnyAsync(a => a.IsActive && a.Type == AuthenticatorType.Totp && a.User != null && a.User.Email == email, ct);
        return CommandResult<TwoFaTotpStatusResult>.Ok(new TwoFaTotpStatusResult(active));
    }
}
