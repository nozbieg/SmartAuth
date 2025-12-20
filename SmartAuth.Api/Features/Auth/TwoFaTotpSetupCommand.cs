using Microsoft.Extensions.Options;
using QRCoder;
using SmartAuth.Api.Services;
using SmartAuth.Api.Utilities;

namespace SmartAuth.Api.Features.Auth;

public record TwoFaTotpSetupCommand(bool ForceRestart = false) : IRequest<CommandResult<TwoFaTotpSetupResult>>;

public record TwoFaTotpSetupResult(Guid SetupId, string Secret, string OtpAuthUri, string QrImageBase64);

public class TwoFaTotpSetupValidator : Validator<TwoFaTotpSetupCommand>
{
    protected override Task ValidateParams(TwoFaTotpSetupCommand request) => Task.CompletedTask;
}

public class TwoFaTotpSetupCommandHandler(
    AuthDbContext db,
    IHttpContextAccessor accessor,
    IMicrosoftAuthenticatorClient msClient,
    IOptions<TotpOptions> totpOptions)
    : IRequestHandler<TwoFaTotpSetupCommand, CommandResult<TwoFaTotpSetupResult>>
{
    public async Task<CommandResult<TwoFaTotpSetupResult>> Handle(TwoFaTotpSetupCommand req, CancellationToken ct)
    {
        var opts = totpOptions.Value;
        if (opts.Digits < 4 || opts.Digits > 10)
            return CommandResult<TwoFaTotpSetupResult>.Fail(Errors.Internal(Messages.TwoFactor.TotpInvalidConfiguration));

        var (_, email, authError) = HandlerHelpers.GetAuthenticatedContext(accessor);
        if (authError is not null)
            return CommandResult<TwoFaTotpSetupResult>.Fail(authError);

        var (user, userError) = await HandlerHelpers.GetUserWithAuthenticatorsAsync(db, email!, ct);
        if (userError is not null)
            return CommandResult<TwoFaTotpSetupResult>.Fail(userError);

        if (req.ForceRestart)
        {
            var allTotp = user!.Authenticators.Where(a => a.Type == AuthenticatorType.Totp).ToList();
            if (allTotp.Count != 0)
            {
                db.UserAuthenticators.RemoveRange(allTotp);
                await db.SaveChangesAsync(ct);
            }
        }

        var pending = user!.Authenticators.Where(a => a.Type == AuthenticatorType.Totp && !a.IsActive).ToList();
        if (pending.Count != 0)
        {
            db.UserAuthenticators.RemoveRange(pending);
            await db.SaveChangesAsync(ct);
        }

        var activeExists = user.Authenticators.Any(a => a.Type == AuthenticatorType.Totp && a.IsActive);
        if (activeExists && !req.ForceRestart)
            return CommandResult<TwoFaTotpSetupResult>.Fail(Errors.Conflict(Messages.TwoFactor.TotpAlreadyEnabled));

        var secret = Totp.GenerateSecret();
        var uri = msClient.BuildOtpAuthUri(email!, secret);

        var auth = new UserAuthenticator
        {
            UserId = user.Id,
            Type = AuthenticatorType.Totp,
            Secret = secret,
            IsActive = false
        };
        db.UserAuthenticators.Add(auth);
        await db.SaveChangesAsync(ct);

        using var qrGen = new QRCodeGenerator();
        var data = qrGen.CreateQrCode(uri, QRCodeGenerator.ECCLevel.Q);
        var pngQr = new PngByteQRCode(data).GetGraphic(10);
        var b64 = Convert.ToBase64String(pngQr);

        return CommandResult<TwoFaTotpSetupResult>.Ok(new TwoFaTotpSetupResult(auth.Id, secret, uri, b64));
    }
}