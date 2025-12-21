using SmartAuth.Api.Contracts;
using SmartAuth.Infrastructure.Biometrics;

namespace SmartAuth.Api.Utilities;

/// <summary>
/// Helper methods for handlers.
/// </summary>
public static class HandlerHelpers
{
    /// <summary>
    /// Gets HTTP context and user email from token.
    /// Returns tuple with context, email and optional error.
    /// </summary>
    public static (HttpContext? Context, string? Email, Error? Error) GetAuthenticatedContext(IHttpContextAccessor accessor)
    {
        var ctx = accessor.HttpContext;
        if (ctx is null)
            return (null, null, Errors.Internal(Messages.System.MissingHttpContext));

        var email = TokenUtilities.GetSubjectFromToken(ctx);
        if (email is null)
            return (ctx, null, Errors.Unauthorized());

        return (ctx, email, null);
    }

    /// <summary>
    /// Checks if Face 2FA feature is enabled.
    /// </summary>
    public static Error? CheckFace2FaEnabled(IConfiguration configuration)
    {
        var flags = configuration.GetSection("FeatureFlags").Get<FeatureFlags>()
                    ?? new FeatureFlags(FeatureFlagsConfig.TwoFaCodeEnabled, FeatureFlagsConfig.TwoFaFaceEnabled, FeatureFlagsConfig.TwoFaVoiceEnabled);
        
        if (!flags.twofa_face)
            return Errors.Forbidden(Messages.TwoFactor.Face2FaDisabled);

        return null;
    }

    /// <summary>
    /// Checks if Voice 2FA feature is enabled.
    /// </summary>
    public static Error? CheckVoice2FaEnabled(IConfiguration configuration)
    {
        var flags = configuration.GetSection("FeatureFlags").Get<FeatureFlags>()
                    ?? new FeatureFlags(FeatureFlagsConfig.TwoFaCodeEnabled, FeatureFlagsConfig.TwoFaFaceEnabled, FeatureFlagsConfig.TwoFaVoiceEnabled);

        if (!flags.twofa_voice)
            return Errors.Forbidden(Messages.TwoFactor.Voice2FaDisabled);

        return null;
    }

    /// <summary>
    /// Decodes face image from Base64 with error handling.
    /// </summary>
    public static (FaceImagePayload Payload, Error? Error) DecodeImagePayload(string imageBase64)
    {
        try
        {
            var payload = ImagePayloadDecoder.DecodeBase64(imageBase64);
            return (payload, null);
        }
        catch (Exception ex)
        {
            return (default, Errors.Validation(ex.Message));
        }
    }

    /// <summary>
    /// Decodes voice sample from Base64 WAV with error handling.
    /// </summary>
    public static (VoiceSamplePayload Payload, Error? Error) DecodeVoicePayload(string audioBase64)
    {
        try
        {
            var payload = VoicePayloadDecoder.DecodeBase64(audioBase64);
            return (payload, null);
        }
        catch (Exception ex)
        {
            return (default, Errors.Validation(ex.Message));
        }
    }

    public static async Task<(VoiceEnrollmentResult? Result, Error? Error)> TryEnrollVoiceAsync(
        IVoiceRecognitionService recognition,
        VoiceSamplePayload payload,
        CancellationToken ct)
    {
        try
        {
            var result = await recognition.EnrollAsync(payload, ct);
            return (result, null);
        }
        catch (VoiceRecognitionException vre)
        {
            return (null, Errors.Validation(vre.Message, new Dictionary<string, object> { ["code"] = vre.Code }));
        }
    }

    public static async Task<(VoiceVerificationResult? Result, Error? Error)> TryVerifyVoiceAsync(
        IVoiceRecognitionService recognition,
        VoiceSamplePayload payload,
        List<UserBiometric> references,
        CancellationToken ct)
    {
        try
        {
            var result = await recognition.VerifyAsync(payload, references, ct);
            return (result, null);
        }
        catch (VoiceRecognitionException vre)
        {
            return (null, Errors.Validation(vre.Message, new Dictionary<string, object> { ["code"] = vre.Code }));
        }
    }

    /// <summary>
    /// Performs face enrollment operation with error handling.
    /// </summary>
    public static async Task<(FaceEnrollmentResult? Result, Error? Error)> TryEnrollFaceAsync(
        IFaceRecognitionService recognition,
        FaceImagePayload payload,
        CancellationToken ct)
    {
        try
        {
            var result = await recognition.EnrollAsync(payload, ct);
            return (result, null);
        }
        catch (FaceRecognitionException fre)
        {
            return (null, Errors.Validation(fre.Message, new Dictionary<string, object> { ["code"] = fre.Code }));
        }
    }

    /// <summary>
    /// Performs face verification operation with error handling.
    /// </summary>
    public static async Task<(FaceVerificationResult? Result, Error? Error)> TryVerifyFaceAsync(
        IFaceRecognitionService recognition,
        FaceImagePayload payload,
        List<UserBiometric> references,
        CancellationToken ct)
    {
        try
        {
            var result = await recognition.VerifyAsync(payload, references, ct);
            return (result, null);
        }
        catch (FaceRecognitionException fre)
        {
            return (null, Errors.Validation(fre.Message, new Dictionary<string, object> { ["code"] = fre.Code }));
        }
    }

    /// <summary>
    /// Gets user by email with Include for Authenticators.
    /// </summary>
    public static async Task<(User? User, Error? Error)> GetUserWithAuthenticatorsAsync(
        AuthDbContext db,
        string email,
        CancellationToken ct)
    {
        var user = await db.Users
            .Include(u => u.Authenticators)
            .FirstOrDefaultAsync(u => u.Email == email, ct);

        if (user is null)
            return (null, Errors.NotFound(nameof(User), email));

        return (user, null);
    }

    /// <summary>
    /// Gets user by email with Include for Biometrics.
    /// </summary>
    public static async Task<(User? User, Error? Error)> GetUserWithBiometricsAsync(
        AuthDbContext db,
        string email,
        CancellationToken ct)
    {
        var user = await db.Users
            .Include(u => u.Biometrics)
            .FirstOrDefaultAsync(u => u.Email == email, ct);

        if (user is null)
            return (null, Errors.NotFound(nameof(User), email));

        return (user, null);
    }
}

