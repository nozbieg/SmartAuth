using Microsoft.EntityFrameworkCore;
using SmartAuth.Api.Contracts;
using SmartAuth.Api.Utilities;
using SmartAuth.Domain.Entities;
using SmartAuth.Infrastructure;
using SmartAuth.Infrastructure.Biometrics;
using SmartAuth.Infrastructure.Commons;

namespace SmartAuth.Api.Features.Auth;

public record TwoFaFaceVerifyCommand(string ImageBase64) : IRequest<CommandResult<TwoFaCodeVerifyResult>>;

public sealed class TwoFaFaceVerifyValidator : Validator<TwoFaFaceVerifyCommand>
{
    protected override Task ValidateParams(TwoFaFaceVerifyCommand request)
    {
        if (string.IsNullOrWhiteSpace(request.ImageBase64))
            Metadata.Add(nameof(request.ImageBase64), "Face image payload is required.");
        return Task.CompletedTask;
    }
}

public sealed class TwoFaFaceVerifyCommandHandler(
    IHttpContextAccessor accessor,
    IFaceRecognitionService recognition,
    IConfiguration configuration)
    : IRequestHandler<TwoFaFaceVerifyCommand, CommandResult<TwoFaCodeVerifyResult>>
{
    public async Task<CommandResult<TwoFaCodeVerifyResult>> Handle(TwoFaFaceVerifyCommand req, CancellationToken ct)
    {
        var ctx = accessor.HttpContext;
        if (ctx is null)
            return CommandResult<TwoFaCodeVerifyResult>.Fail(Errors.Internal("Missing HttpContext"));

        var flags = configuration.GetSection("FeatureFlags").Get<FeatureFlags>()
                    ?? new FeatureFlags(FeatureFlagsConfig.TwoFaCodeEnabled, FeatureFlagsConfig.TwoFaFaceEnabled);
        if (!flags.twofa_face)
            return CommandResult<TwoFaCodeVerifyResult>.Fail(Errors.Forbidden("face_2fa_disabled"));

        var email = TokenUtilities.GetSubjectFromToken(ctx);
        if (email is null)
            return CommandResult<TwoFaCodeVerifyResult>.Fail(Errors.Unauthorized());

        var db = ctx.RequestServices.GetRequiredService<AuthDbContext>();
        var user = await db.Users.Include(u => u.Biometrics).FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null)
            return CommandResult<TwoFaCodeVerifyResult>.Fail(Errors.NotFound(nameof(User), email));

        var faceReferences = user.Biometrics.Where(b => b.Kind == AuthenticatorType.Face).ToList();
        if (faceReferences.Count == 0)
            return CommandResult<TwoFaCodeVerifyResult>.Fail(Errors.NotFound("FaceBiometric", email));

        FaceImagePayload payload;
        try
        {
            payload = ImagePayloadDecoder.DecodeBase64(req.ImageBase64);
        }
        catch (Exception ex)
        {
            return CommandResult<TwoFaCodeVerifyResult>.Fail(Errors.Validation(ex.Message));
        }

        FaceVerificationResult verification;
        try
        {
            verification = await recognition.VerifyAsync(payload, faceReferences, ct);
        }
        catch (FaceRecognitionException fre)
        {
            return CommandResult<TwoFaCodeVerifyResult>.Fail(Errors.Validation(fre.Message, new Dictionary<string, object>
            {
                ["code"] = fre.Code
            }));
        }

        verification.MatchedBiometric.QualityScore = verification.Analysis.Quality.Overall;
        verification.MatchedBiometric.IsActive = true;
        await db.SaveChangesAsync(ct);

        var jwt = TokenUtilities.IssueAccessToken(configuration, email, email);
        return CommandResult<TwoFaCodeVerifyResult>.Ok(new TwoFaCodeVerifyResult(jwt));
    }
}
