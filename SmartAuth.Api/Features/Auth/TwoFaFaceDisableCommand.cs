using SmartAuth.Api.Utilities;

namespace SmartAuth.Api.Features.Auth;

public record TwoFaFaceDisableCommand() : IRequest<CommandResult>;

public sealed class TwoFaFaceDisableCommandHandler(IHttpContextAccessor accessor)
    : IRequestHandler<TwoFaFaceDisableCommand, CommandResult>
{
    public async Task<CommandResult> Handle(TwoFaFaceDisableCommand request, CancellationToken ct)
    {
        var (ctx, email, authError) = HandlerHelpers.GetAuthenticatedContext(accessor);
        if (authError is not null)
            return CommandResult.Fail(authError);

        var db = ctx!.RequestServices.GetRequiredService<AuthDbContext>();
        var (user, userError) = await HandlerHelpers.GetUserWithBiometricsAsync(db, email!, ct);
        if (userError is not null)
            return CommandResult.Fail(userError);

        var affected = user!.Biometrics.Where(b => b.Kind == AuthenticatorType.Face && b.IsActive).ToList();
        if (affected.Count == 0)
            return CommandResult.Fail(Errors.NotFound("FaceBiometric", email!));

        foreach (var bio in affected)
        {
            bio.IsActive = false;
        }

        await db.SaveChangesAsync(ct);
        return CommandResult.Ok();
    }
}
