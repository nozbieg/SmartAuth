using Microsoft.EntityFrameworkCore;
using SmartAuth.Api.Contracts;
using SmartAuth.Api.Utilities;
using SmartAuth.Domain.Entities;
using SmartAuth.Infrastructure;
using SmartAuth.Infrastructure.Biometrics;
using SmartAuth.Infrastructure.Commons;

namespace SmartAuth.Api.Features.Auth;

public record TwoFaFaceEnrollCommand(string ImageBase64) : IRequest<CommandResult<TwoFaFaceEnrollResult>>;

public record TwoFaFaceEnrollResult(Guid BiometricId, double QualityScore, double LivenessScore, string ModelVersion);

public sealed class TwoFaFaceEnrollValidator : Validator<TwoFaFaceEnrollCommand>
{
    protected override Task ValidateParams(TwoFaFaceEnrollCommand request)
    {
        if (string.IsNullOrWhiteSpace(request.ImageBase64))
            Metadata.Add(nameof(request.ImageBase64), "Face image payload is required.");
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

        var ctx = accessor.HttpContext;
        if (ctx is null)
            return CommandResult<TwoFaFaceEnrollResult>.Fail(Errors.Internal("Missing HttpContext"));

        var flags = configuration.GetSection("FeatureFlags").Get<FeatureFlags>()
                    ?? new FeatureFlags(FeatureFlagsConfig.TwoFaCodeEnabled, FeatureFlagsConfig.TwoFaFaceEnabled, FeatureFlagsConfig.TwoFaVoiceEnabled);
        if (!flags.twofa_face)
            return CommandResult<TwoFaFaceEnrollResult>.Fail(Errors.Forbidden("face_2fa_disabled"));

        var email = TokenUtilities.GetSubjectFromToken(ctx);
        if (email is null)
            return CommandResult<TwoFaFaceEnrollResult>.Fail(Errors.Unauthorized());

        var db = ctx.RequestServices.GetRequiredService<AuthDbContext>();
        var user = await db.Users.Include(u => u.Biometrics).FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null)
            return CommandResult<TwoFaFaceEnrollResult>.Fail(Errors.NotFound(nameof(User), email));

        FaceImagePayload payload;
        try
        {
            payload = ImagePayloadDecoder.DecodeBase64(req.ImageBase64);
        }
        catch (Exception ex)
        {
            return CommandResult<TwoFaFaceEnrollResult>.Fail(Errors.Validation(ex.Message));
        }


        FaceEnrollmentResult enrollment;
        try
        {
            enrollment = await recognition.EnrollAsync(payload, ct);
        }
        catch (FaceRecognitionException fre)
        {
            return CommandResult<TwoFaFaceEnrollResult>.Fail(Errors.Validation(fre.Message, new Dictionary<string, object>
            {
                ["code"] = fre.Code
            }));
        }

        foreach (var existing in user.Biometrics.Where(b => b.Kind == AuthenticatorType.Face))
        {
            existing.IsActive = false;
        }

        var biometrics = enrollment.Analysis.Embedding.Embedding;
        var bio = new UserBiometric
        {
            UserId = user.Id,
            Kind = AuthenticatorType.Face,
            Embedding = biometrics,
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
