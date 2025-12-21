using SmartAuth.Api.Contracts;
using SmartAuth.Api.Utilities;
using SmartAuth.Domain.Entities;
using SmartAuth.Infrastructure.Biometrics;

namespace SmartAuth.Api.Features.Auth;

public record TwoFaVoiceVerifyCommand(string AudioBase64) : IRequest<CommandResult<TwoFaCodeVerifyResult>>;

public sealed class TwoFaVoiceVerifyValidator : Validator<TwoFaVoiceVerifyCommand>
{
    protected override Task ValidateParams(TwoFaVoiceVerifyCommand request)
    {
        if (string.IsNullOrWhiteSpace(request.AudioBase64))
            Metadata.Add(nameof(request.AudioBase64), Messages.Biometrics.AudioRequired);
        return Task.CompletedTask;
    }
}

public sealed class TwoFaVoiceVerifyCommandHandler(
    IHttpContextAccessor accessor,
    IVoiceRecognitionService recognition,
    IConfiguration configuration)
    : IRequestHandler<TwoFaVoiceVerifyCommand, CommandResult<TwoFaCodeVerifyResult>>
{
    public async Task<CommandResult<TwoFaCodeVerifyResult>> Handle(TwoFaVoiceVerifyCommand req, CancellationToken ct)
    {
        var (ctx, email, authError) = HandlerHelpers.GetAuthenticatedContext(accessor);
        if (authError is not null)
            return CommandResult<TwoFaCodeVerifyResult>.Fail(authError);

        var featureError = HandlerHelpers.CheckVoice2FaEnabled(configuration);
        if (featureError is not null)
            return CommandResult<TwoFaCodeVerifyResult>.Fail(featureError);

        var db = ctx!.RequestServices.GetRequiredService<AuthDbContext>();
        var (user, userError) = await HandlerHelpers.GetUserWithBiometricsAsync(db, email!, ct);
        if (userError is not null)
            return CommandResult<TwoFaCodeVerifyResult>.Fail(userError);

        var references = user!.Biometrics.Where(b => b.Kind == AuthenticatorType.Voice).ToList();
        if (references.Count == 0)
            return CommandResult<TwoFaCodeVerifyResult>.Fail(Errors.NotFound("VoiceBiometric", email!));

        var (payload, decodeError) = HandlerHelpers.DecodeVoicePayload(req.AudioBase64);
        if (decodeError is not null)
            return CommandResult<TwoFaCodeVerifyResult>.Fail(decodeError);

        var (verification, verifyError) = await HandlerHelpers.TryVerifyVoiceAsync(recognition, payload, references, ct);
        if (verifyError is not null)
            return CommandResult<TwoFaCodeVerifyResult>.Fail(verifyError);

        verification!.MatchedBiometric.IsActive = true;
        verification.MatchedBiometric.AudioDurationSeconds = payload.DurationSeconds;
        verification.MatchedBiometric.AudioSampleRate = payload.SampleRate;
        verification.MatchedBiometric.QualityScore = verification.Quality.Overall;
        await db.SaveChangesAsync(ct);

        var jwt = TokenUtilities.IssueAccessToken(configuration, email!, email!);
        return CommandResult<TwoFaCodeVerifyResult>.Ok(new TwoFaCodeVerifyResult(jwt));
    }
}
