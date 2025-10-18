using Microsoft.Extensions.Options;
using SmartAuth.Api.Utilities;
using SmartAuth.Infrastructure.Security;
using QRCoder;
using SmartAuth.Api.Services;

namespace SmartAuth.Api.Features.Auth;

public record TwoFaTotpSetupCommand(bool ForceRestart = false) : IRequest<CommandResult<TwoFaTotpSetupResult>>;
public record TwoFaTotpSetupResult(Guid SetupId, string Secret, string OtpAuthUri, string QrImageBase64);

public class TwoFaTotpSetupValidator : Validator<TwoFaTotpSetupCommand>
{
    protected override Task ValidateParams(TwoFaTotpSetupCommand request) => Task.CompletedTask;
}

public class TwoFaTotpSetupCommandHandler(AuthDbContext db, IHttpContextAccessor accessor, IMicrosoftAuthenticatorClient msClient, IOptions<TotpOptions> totpOptions)
    : IRequestHandler<TwoFaTotpSetupCommand, CommandResult<TwoFaTotpSetupResult>>
{
    public async Task<CommandResult<TwoFaTotpSetupResult>> Handle(TwoFaTotpSetupCommand req, CancellationToken ct)
    {
        var opts = totpOptions.Value;
        if (opts.Digits < 4 || opts.Digits > 10)
            return CommandResult<TwoFaTotpSetupResult>.Fail(Errors.Internal("Invalid TOTP digits configuration"));

        var ctx = accessor.HttpContext;
        if (ctx is null) return CommandResult<TwoFaTotpSetupResult>.Fail(Errors.Internal("Missing HttpContext"));
        var email = TokenUtilities.GetSubjectFromToken(ctx);
        if (email is null) return CommandResult<TwoFaTotpSetupResult>.Fail(Errors.Unauthorized());

        var user = await db.Users.Include(u => u.Authenticators).FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null) return CommandResult<TwoFaTotpSetupResult>.Fail(Errors.NotFound("User", email));

        if (req.ForceRestart)
        {
            var allTotp = user.Authenticators.Where(a => a.Type == AuthenticatorType.Totp).ToList();
            if (allTotp.Any())
            {
                db.UserAuthenticators.RemoveRange(allTotp);
                await db.SaveChangesAsync(ct);
            }
        }

        var pending = user.Authenticators.Where(a => a.Type == AuthenticatorType.Totp && !a.IsActive).ToList();
        if (pending.Any())
        {
            db.UserAuthenticators.RemoveRange(pending);
            await db.SaveChangesAsync(ct);
        }

        var activeExists = user.Authenticators.Any(a => a.Type == AuthenticatorType.Totp && a.IsActive);
        if (activeExists && !req.ForceRestart)
            return CommandResult<TwoFaTotpSetupResult>.Fail(Errors.Conflict("totp_already_enabled"));

        var secret = SmartAuth.Infrastructure.Security.Totp.GenerateSecret();
        var uri = msClient.BuildOtpAuthUri(email, secret);

        var auth = new UserAuthenticator
        {
            UserId = user.Id,
            Type = AuthenticatorType.Totp,
            Secret = secret,
            IsActive = false
        };
        db.UserAuthenticators.Add(auth);
        await db.SaveChangesAsync(ct);

        // Generacja QR (PNG base64)
        using var qrGen = new QRCodeGenerator();
        var data = qrGen.CreateQrCode(uri, QRCodeGenerator.ECCLevel.Q);
        var pngQr = new PngByteQRCode(data).GetGraphic(10);
        var b64 = Convert.ToBase64String((byte[])pngQr);

        return CommandResult<TwoFaTotpSetupResult>.Ok(new TwoFaTotpSetupResult(auth.Id, secret, uri, b64));
    }
}