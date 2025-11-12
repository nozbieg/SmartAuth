using Microsoft.EntityFrameworkCore;
using SmartAuth.Api.Utilities;
using SmartAuth.Domain.Entities;
using SmartAuth.Infrastructure;
using SmartAuth.Infrastructure.Commons;

namespace SmartAuth.Api.Features.Auth;

public record TwoFaFaceDisableCommand() : IRequest<CommandResult>;

public sealed class TwoFaFaceDisableCommandHandler(IHttpContextAccessor accessor)
    : IRequestHandler<TwoFaFaceDisableCommand, CommandResult>
{
    public async Task<CommandResult> Handle(TwoFaFaceDisableCommand request, CancellationToken ct)
    {
        var ctx = accessor.HttpContext;
        if (ctx is null)
            return CommandResult.Fail(Errors.Internal("Missing HttpContext"));

        var email = TokenUtilities.GetSubjectFromToken(ctx);
        if (email is null)
            return CommandResult.Fail(Errors.Unauthorized());

        var db = ctx.RequestServices.GetRequiredService<AuthDbContext>();
        var user = await db.Users.Include(u => u.Biometrics).FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null)
            return CommandResult.Fail(Errors.NotFound(nameof(User), email));

        var affected = user.Biometrics.Where(b => b.Kind == AuthenticatorType.Face && b.IsActive).ToList();
        if (affected.Count == 0)
            return CommandResult.Fail(Errors.NotFound("FaceBiometric", email));

        foreach (var bio in affected)
        {
            bio.IsActive = false;
        }

        await db.SaveChangesAsync(ct);
        return CommandResult.Ok();
    }
}
