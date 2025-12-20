using SmartAuth.Api.Contracts;
using SmartAuth.Api.Utilities;
using SmartAuth.Infrastructure.Biometrics;

namespace SmartAuth.Api.Features.Auth;

public record TwoFaFaceEnrollCommand(string ImageBase64) : IRequest<CommandResult<TwoFaFaceEnrollResult>>;

public record TwoFaFaceEnrollResult(Guid BiometricId, double QualityScore, double LivenessScore, string ModelVersion);

public sealed class TwoFaFaceEnrollValidator : Validator<TwoFaFaceEnrollCommand>
{
    protected override Task ValidateParams(TwoFaFaceEnrollCommand request)
    {
        if (string.IsNullOrWhiteSpace(request.ImageBase64))
            Metadata.Add(nameof(request.ImageBase64), Messages.Validation.FaceImageRequired);
        return Task.CompletedTask;
    }
}

public sealed class TwoFaFaceEnrollCommandHandler(
    IHttpContextAccessor accessor,
    IFaceRecognitionService recognition,
    IConfiguration configuration)
    : IRequestHandler<TwoFaFaceEnrollCommand, CommandResult<TwoFaFaceEnrollResult>>
{
    public async Task<CommandResult<TwoFaFaceEnrollResult>> Handle(TwoFaFaceEnrollCommand req, CancellationToken ct)
    {
        var (ctx, email, authError) = HandlerHelpers.GetAuthenticatedContext(accessor);
        if (authError is not null)
            return CommandResult<TwoFaFaceEnrollResult>.Fail(authError);

        var featureError = HandlerHelpers.CheckFace2FaEnabled(configuration);
        if (featureError is not null)
            return CommandResult<TwoFaFaceEnrollResult>.Fail(featureError);

        var db = ctx!.RequestServices.GetRequiredService<AuthDbContext>();
        var (user, userError) = await HandlerHelpers.GetUserWithBiometricsAsync(db, email!, ct);
        if (userError is not null)
            return CommandResult<TwoFaFaceEnrollResult>.Fail(userError);

        var (payload, decodeError) = HandlerHelpers.DecodeImagePayload(req.ImageBase64);
        if (decodeError is not null)
            return CommandResult<TwoFaFaceEnrollResult>.Fail(decodeError);

        var (enrollment, enrollError) = await HandlerHelpers.TryEnrollFaceAsync(recognition, payload, ct);
        if (enrollError is not null)
            return CommandResult<TwoFaFaceEnrollResult>.Fail(enrollError);

        foreach (var existing in user!.Biometrics.Where(b => b.Kind == AuthenticatorType.Face))
        {
            existing.IsActive = false;
        }

        var bio = new UserBiometric
        {
            UserId = user.Id,
            Kind = AuthenticatorType.Face,
            Embedding = enrollment!.Analysis.Embedding.Embedding,
            Version = enrollment.Analysis.Embedding.ModelVersion,
            QualityScore = enrollment.Analysis.Quality.Overall,
            LivenessMethod = enrollment.Analysis.Liveness.Method,
            IsActive = true
        };

        db.UserBiometrics.Add(bio);
        await db.SaveChangesAsync(ct);

        return CommandResult<TwoFaFaceEnrollResult>.Ok(new TwoFaFaceEnrollResult(
            bio.Id,
            bio.QualityScore,
            enrollment.Analysis.Liveness.Score,
            bio.Version));
    }
}
