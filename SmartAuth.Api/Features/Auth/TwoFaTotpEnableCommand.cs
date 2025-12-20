using SmartAuth.Api.Utilities;
using SmartAuth.Infrastructure.Security;

namespace SmartAuth.Api.Features.Auth;

public record TwoFaTotpEnableCommand(Guid SetupId, string Code) : IRequest<CommandResult<TwoFaTotpEnableResult>>;
public record TwoFaTotpEnableResult(string Message = "TOTP enabled");

public class TwoFaTotpEnableValidator : Validator<TwoFaTotpEnableCommand>
{
    protected override Task ValidateParams(TwoFaTotpEnableCommand request)
    {
        if (request.SetupId == Guid.Empty) Metadata.Add(nameof(request.SetupId), "SetupId jest wymagany");
        if (string.IsNullOrWhiteSpace(request.Code)) Metadata.Add(nameof(request.Code), "Kod jest wymagany");
        return Task.CompletedTask;
    }
}

public class TwoFaTotpEnableCommandHandler(AuthDbContext db, IHttpContextAccessor accessor)
    : IRequestHandler<TwoFaTotpEnableCommand, CommandResult<TwoFaTotpEnableResult>>
{
    public async Task<CommandResult<TwoFaTotpEnableResult>> Handle(TwoFaTotpEnableCommand req, CancellationToken ct)
    {
        var ctx = accessor.HttpContext;
        if (ctx is null) return CommandResult<TwoFaTotpEnableResult>.Fail(Errors.Internal("Brak kontekstu HTTP"));
        var email = TokenUtilities.GetSubjectFromToken(ctx);
        if (email is null) return CommandResult<TwoFaTotpEnableResult>.Fail(Errors.Unauthorized());

        var user = await db.Users.Include(u => u.Authenticators).FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null) return CommandResult<TwoFaTotpEnableResult>.Fail(Errors.NotFound("User", email));

        var pending = user.Authenticators.FirstOrDefault(a => a.Id == req.SetupId && !a.IsActive && a.Type == AuthenticatorType.Totp);
        if (pending is null) return CommandResult<TwoFaTotpEnableResult>.Fail(Errors.NotFound("PendingTotp", req.SetupId.ToString()));

        var ok = Totp.ValidateCode(pending.Secret, req.Code);
        if (!ok) return CommandResult<TwoFaTotpEnableResult>.Fail(Errors.Unauthorized());

        pending.IsActive = true;
        pending.LastUsedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return CommandResult<TwoFaTotpEnableResult>.Ok(new TwoFaTotpEnableResult());
    }
}

