using SmartAuth.Api.Utilities;
using SmartAuth.Domain.Entities;

namespace SmartAuth.Api.Features.Auth;

public record TwoFaVoiceDisableCommand() : IRequest<CommandResult>;

public sealed class TwoFaVoiceDisableCommandHandler(IHttpContextAccessor accessor)
    : IRequestHandler<TwoFaVoiceDisableCommand, CommandResult>
{
    public async Task<CommandResult> Handle(TwoFaVoiceDisableCommand request, CancellationToken ct)
    {
        var (ctx, email, authError) = HandlerHelpers.GetAuthenticatedContext(accessor);
        if (authError is not null)
            return CommandResult.Fail(authError);

        var db = ctx!.RequestServices.GetRequiredService<AuthDbContext>();
        var (user, userError) = await HandlerHelpers.GetUserWithBiometricsAsync(db, email!, ct);
        if (userError is not null)
            return CommandResult.Fail(userError);

        var affected = user!.Biometrics.Where(b => b.Kind == AuthenticatorType.Voice && b.IsActive).ToList();
        if (affected.Count == 0)
            return CommandResult.Fail(Errors.NotFound("VoiceBiometric", email!));

        foreach (var bio in affected) bio.IsActive = false;

        await db.SaveChangesAsync(ct);
        return CommandResult.Ok();
    }
}
