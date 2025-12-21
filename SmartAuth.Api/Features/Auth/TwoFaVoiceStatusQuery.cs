using SmartAuth.Api.Utilities;
using SmartAuth.Domain.Entities;

namespace SmartAuth.Api.Features.Auth;

public record TwoFaVoiceStatusQuery() : IRequest<CommandResult<TwoFaVoiceStatusResult>>;

public record TwoFaVoiceStatusResult(bool Enabled, int ActiveCount);

public sealed class TwoFaVoiceStatusQueryHandler(IHttpContextAccessor accessor)
    : IRequestHandler<TwoFaVoiceStatusQuery, CommandResult<TwoFaVoiceStatusResult>>
{
    public async Task<CommandResult<TwoFaVoiceStatusResult>> Handle(TwoFaVoiceStatusQuery req, CancellationToken ct)
    {
        var (ctx, email, authError) = HandlerHelpers.GetAuthenticatedContext(accessor);
        if (authError is not null)
            return CommandResult<TwoFaVoiceStatusResult>.Fail(authError);

        var db = ctx!.RequestServices.GetRequiredService<AuthDbContext>();
        var activeCount = await db.UserBiometrics.CountAsync(
            b => b.User != null && b.User.Email == email && b.Kind == AuthenticatorType.Voice && b.IsActive, ct);

        return CommandResult<TwoFaVoiceStatusResult>.Ok(new TwoFaVoiceStatusResult(activeCount > 0, activeCount));
    }
}
