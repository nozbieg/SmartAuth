using SmartAuth.Api.Utilities;

namespace SmartAuth.Api.Features.Auth;

public record TwoFaFaceStatusQuery() : IRequest<CommandResult<TwoFaFaceStatusResult>>;

public record TwoFaFaceStatusResult(bool Enabled, int ActiveCount);

public sealed class TwoFaFaceStatusQueryHandler(IHttpContextAccessor accessor)
    : IRequestHandler<TwoFaFaceStatusQuery, CommandResult<TwoFaFaceStatusResult>>
{
    public async Task<CommandResult<TwoFaFaceStatusResult>> Handle(TwoFaFaceStatusQuery req, CancellationToken ct)
    {
        var (ctx, email, authError) = HandlerHelpers.GetAuthenticatedContext(accessor);
        if (authError is not null)
            return CommandResult<TwoFaFaceStatusResult>.Fail(authError);

        var db = ctx!.RequestServices.GetRequiredService<AuthDbContext>();
        var activeCount = await db.UserBiometrics.CountAsync(
            b => b.User != null && b.User.Email == email && b.Kind == AuthenticatorType.Face && b.IsActive, ct);
        
        return CommandResult<TwoFaFaceStatusResult>.Ok(new TwoFaFaceStatusResult(activeCount > 0, activeCount));
    }
}
