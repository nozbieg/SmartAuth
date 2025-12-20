using SmartAuth.Api.Utilities;

namespace SmartAuth.Api.Features.Auth;

public record TwoFaFaceStatusQuery() : IRequest<CommandResult<TwoFaFaceStatusResult>>;

public record TwoFaFaceStatusResult(bool Enabled, int ActiveCount);

public sealed class TwoFaFaceStatusQueryHandler(IHttpContextAccessor accessor)
    : IRequestHandler<TwoFaFaceStatusQuery, CommandResult<TwoFaFaceStatusResult>>
{
    public async Task<CommandResult<TwoFaFaceStatusResult>> Handle(TwoFaFaceStatusQuery req, CancellationToken ct)
    {
        var ctx = accessor.HttpContext;
        if (ctx is null)
            return CommandResult<TwoFaFaceStatusResult>.Fail(Errors.Internal(Messages.System.MissingHttpContext));

        var email = TokenUtilities.GetSubjectFromToken(ctx);
        if (email is null)
            return CommandResult<TwoFaFaceStatusResult>.Fail(Errors.Unauthorized());

        var db = ctx.RequestServices.GetRequiredService<AuthDbContext>();
        var activeCount = await db.UserBiometrics.CountAsync(b => b.User != null && b.User.Email == email && b.Kind == AuthenticatorType.Face && b.IsActive, ct);
        return CommandResult<TwoFaFaceStatusResult>.Ok(new TwoFaFaceStatusResult(activeCount > 0, activeCount));
    }
}
