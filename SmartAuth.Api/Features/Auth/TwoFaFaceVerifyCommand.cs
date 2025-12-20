using SmartAuth.Api.Contracts;
using SmartAuth.Api.Utilities;
using SmartAuth.Infrastructure.Biometrics;

namespace SmartAuth.Api.Features.Auth;

public record TwoFaFaceVerifyCommand(string ImageBase64) : IRequest<CommandResult<TwoFaCodeVerifyResult>>;

public sealed class TwoFaFaceVerifyValidator : Validator<TwoFaFaceVerifyCommand>
{
    protected override Task ValidateParams(TwoFaFaceVerifyCommand request)
    {
        if (string.IsNullOrWhiteSpace(request.ImageBase64))
            Metadata.Add(nameof(request.ImageBase64), Messages.Validation.FaceImageRequired);
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
        var (ctx, email, authError) = HandlerHelpers.GetAuthenticatedContext(accessor);
        if (authError is not null)
            return CommandResult<TwoFaCodeVerifyResult>.Fail(authError);

        var featureError = HandlerHelpers.CheckFace2FaEnabled(configuration);
        if (featureError is not null)
            return CommandResult<TwoFaCodeVerifyResult>.Fail(featureError);

        var db = ctx!.RequestServices.GetRequiredService<AuthDbContext>();
        var (user, userError) = await HandlerHelpers.GetUserWithBiometricsAsync(db, email!, ct);
        if (userError is not null)
            return CommandResult<TwoFaCodeVerifyResult>.Fail(userError);

        var faceReferences = user!.Biometrics.Where(b => b.Kind == AuthenticatorType.Face).ToList();
        if (faceReferences.Count == 0)
            return CommandResult<TwoFaCodeVerifyResult>.Fail(Errors.NotFound("FaceBiometric", email!));

        var (payload, decodeError) = HandlerHelpers.DecodeImagePayload(req.ImageBase64);
        if (decodeError is not null)
            return CommandResult<TwoFaCodeVerifyResult>.Fail(decodeError);

        var (verification, verifyError) = await HandlerHelpers.TryVerifyFaceAsync(recognition, payload, faceReferences, ct);
        if (verifyError is not null)
            return CommandResult<TwoFaCodeVerifyResult>.Fail(verifyError);

        verification!.MatchedBiometric.QualityScore = verification.Analysis.Quality.Overall;
        verification.MatchedBiometric.IsActive = true;
        await db.SaveChangesAsync(ct);

        var jwt = TokenUtilities.IssueAccessToken(configuration, email!, email!);
        return CommandResult<TwoFaCodeVerifyResult>.Ok(new TwoFaCodeVerifyResult(jwt));
    }
}
