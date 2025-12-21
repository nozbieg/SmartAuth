using SmartAuth.Api.Utilities;
using SmartAuth.Domain.Entities;
using SmartAuth.Infrastructure.Biometrics;

namespace SmartAuth.Api.Features.Auth;

public record TwoFaVoiceEnrollCommand(string AudioBase64) : IRequest<CommandResult<TwoFaVoiceEnrollResult>>;

public record TwoFaVoiceEnrollResult(Guid BiometricId, double QualityScore, double DurationSeconds, string ModelVersion);

public sealed class TwoFaVoiceEnrollValidator : Validator<TwoFaVoiceEnrollCommand>
{
    protected override Task ValidateParams(TwoFaVoiceEnrollCommand request)
    {
        if (string.IsNullOrWhiteSpace(request.AudioBase64))
            Metadata.Add(nameof(request.AudioBase64), Messages.Biometrics.AudioRequired);
        return Task.CompletedTask;
    }
}

public sealed class TwoFaVoiceEnrollCommandHandler(
    IHttpContextAccessor accessor,
    IVoiceRecognitionService recognition,
    IConfiguration configuration)
    : IRequestHandler<TwoFaVoiceEnrollCommand, CommandResult<TwoFaVoiceEnrollResult>>
{
    public async Task<CommandResult<TwoFaVoiceEnrollResult>> Handle(TwoFaVoiceEnrollCommand req, CancellationToken ct)
    {
        var (ctx, email, authError) = HandlerHelpers.GetAuthenticatedContext(accessor);
        if (authError is not null)
            return CommandResult<TwoFaVoiceEnrollResult>.Fail(authError);

        var featureError = HandlerHelpers.CheckVoice2FaEnabled(configuration);
        if (featureError is not null)
            return CommandResult<TwoFaVoiceEnrollResult>.Fail(featureError);

        var db = ctx!.RequestServices.GetRequiredService<AuthDbContext>();
        var (user, userError) = await HandlerHelpers.GetUserWithBiometricsAsync(db, email!, ct);
        if (userError is not null)
            return CommandResult<TwoFaVoiceEnrollResult>.Fail(userError);

        var (payload, decodeError) = HandlerHelpers.DecodeVoicePayload(req.AudioBase64);
        if (decodeError is not null)
            return CommandResult<TwoFaVoiceEnrollResult>.Fail(decodeError);

        var (enrollment, enrollError) = await HandlerHelpers.TryEnrollVoiceAsync(recognition, payload, ct);
        if (enrollError is not null)
            return CommandResult<TwoFaVoiceEnrollResult>.Fail(enrollError);

        foreach (var existing in user!.Biometrics.Where(b => b.Kind == AuthenticatorType.Voice)) existing.IsActive = false;

        var bio = new UserBiometric
        {
            UserId = user.Id,
            Kind = AuthenticatorType.Voice,
            Embedding = enrollment!.Embedding.Embedding,
            Version = enrollment.Embedding.ModelVersion,
            QualityScore = enrollment.Quality.Overall,
            AudioDurationSeconds = payload.DurationSeconds,
            AudioSampleRate = payload.SampleRate,
            IsActive = true
        };

        db.UserBiometrics.Add(bio);
        await db.SaveChangesAsync(ct);

        return CommandResult<TwoFaVoiceEnrollResult>.Ok(new TwoFaVoiceEnrollResult(
            bio.Id,
            enrollment.Quality.Overall,
            payload.DurationSeconds,
            bio.Version));
    }
}
