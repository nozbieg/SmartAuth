using SmartAuth.Domain.Entities;
using SmartAuth.Infrastructure.Commons;

namespace SmartAuth.Infrastructure.Biometrics;

public sealed class VoiceRecognitionService(
    IVoiceEmbedder embedder,
    IVoiceMatcher matcher,
    IVoicePolicy policy)
    : IVoiceRecognitionService
{
    public async Task<VoiceEnrollmentResult> EnrollAsync(VoiceSamplePayload sample, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var quality = EvaluateQuality(sample);
        if (!policy.ValidateQuality(quality, out var qualityReason))
            throw new VoiceRecognitionException(qualityReason ?? "voice.quality_rejected", Messages.Biometrics.AudioTooShort);

        var embedding = await embedder.EmbedAsync(sample, ct);
        var normalized = matcher.Normalize(embedding.Embedding);
        return new VoiceEnrollmentResult(new VoiceEmbedding(normalized, embedding.ModelVersion), quality);
    }

    public async Task<VoiceVerificationResult> VerifyAsync(VoiceSamplePayload sample, IReadOnlyList<UserBiometric> references, CancellationToken ct = default)
    {
        if (references.Count == 0)
            throw new VoiceRecognitionException("voice.no_reference", Messages.Biometrics.NoVoiceReference);

        var quality = EvaluateQuality(sample);
        if (!policy.ValidateQuality(quality, out var qualityReason))
            throw new VoiceRecognitionException(qualityReason ?? "voice.quality_rejected", Messages.Biometrics.AudioTooShort);

        var embedding = await embedder.EmbedAsync(sample, ct);
        var normalized = matcher.Normalize(embedding.Embedding);

        VoiceMatchResult? bestMatch = null;
        UserBiometric? bestBiometric = null;

        foreach (var biometric in references.Where(b => b.IsActive))
        {
            var match = matcher.Decide(normalized, biometric.Embedding, policy.VoiceSimilarityThreshold);
            if (bestMatch is null || match.Similarity > bestMatch.Similarity)
            {
                bestMatch = match;
                bestBiometric = biometric;
            }
        }

        if (bestMatch is null || bestBiometric is null)
            throw new VoiceRecognitionException("voice.no_active_reference", Messages.Biometrics.NoVoiceReference);

        if (!policy.ValidateMatch(bestMatch, out var reason))
            throw new VoiceRecognitionException(reason ?? "voice.match_failed", Messages.Biometrics.VoiceMatchFailed);

        return new VoiceVerificationResult(new VoiceEmbedding(normalized, embedding.ModelVersion), quality, bestMatch, bestBiometric);
    }

    private static VoiceQuality EvaluateQuality(VoiceSamplePayload sample)
    {
        if (sample.SampleRate <= 0 || sample.Samples.Length == 0)
            return new VoiceQuality(0, 0, 0);

        var rms = Math.Sqrt(sample.Samples.Select(s => s * s).DefaultIfEmpty(0).Average());
        var duration = sample.DurationSeconds;
        var normalizedEnergy = Math.Clamp(rms * 4, 0, 1); // prosta normalizacja
        var normalizedDuration = Math.Clamp(duration / 4.0, 0, 1);
        var overall = (normalizedEnergy * 0.5) + (normalizedDuration * 0.5);
        return new VoiceQuality(duration, rms, overall);
    }
}
