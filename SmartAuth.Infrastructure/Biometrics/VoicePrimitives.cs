using Pgvector;
using SmartAuth.Domain.Entities;

namespace SmartAuth.Infrastructure.Biometrics;

public readonly record struct VoiceSamplePayload(int SampleRate, int Channels, float[] Samples)
{
    public double DurationSeconds => Samples.Length == 0 || SampleRate <= 0
        ? 0
        : Samples.Length / (double)(SampleRate * Math.Max(1, Channels));
}

public sealed record VoiceEmbedding(Vector Embedding, string ModelVersion);

public sealed record VoiceQuality(double DurationSeconds, double EnergyRms, double Overall)
{
    public bool IsAcceptable(double minDuration, double minEnergy) =>
        DurationSeconds >= minDuration && EnergyRms >= minEnergy;
}

public sealed record VoiceMatchResult(bool IsMatch, double Similarity, double Threshold);

public sealed record VoiceEnrollmentResult(VoiceEmbedding Embedding, VoiceQuality Quality);

public sealed record VoiceVerificationResult(
    VoiceEmbedding Embedding,
    VoiceQuality Quality,
    VoiceMatchResult Match,
    UserBiometric MatchedBiometric);

public interface IVoiceEmbedder
{
    Task<VoiceEmbedding> EmbedAsync(VoiceSamplePayload sample, CancellationToken ct = default);
}

public interface IVoiceMatcher
{
    double ComputeSimilarity(Vector a, Vector b);
    Vector Normalize(Vector v);
    VoiceMatchResult Decide(Vector probe, Vector candidate, double threshold);
}

public interface IVoicePolicy
{
    double MinDurationSeconds { get; }
    double MinEnergyRms { get; }
    double VoiceSimilarityThreshold { get; }
    int MaxRetries { get; }
    TimeSpan Cooldown { get; }
    bool ValidateQuality(VoiceQuality metrics, out string? reason);
    bool ValidateMatch(VoiceMatchResult match, out string? reason);
}

public interface IVoiceRecognitionService
{
    Task<VoiceEnrollmentResult> EnrollAsync(VoiceSamplePayload sample, CancellationToken ct = default);
    Task<VoiceVerificationResult> VerifyAsync(VoiceSamplePayload sample, IReadOnlyList<UserBiometric> references, CancellationToken ct = default);
}
