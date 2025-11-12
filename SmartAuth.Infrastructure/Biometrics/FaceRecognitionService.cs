using SmartAuth.Domain.Entities;

namespace SmartAuth.Infrastructure.Biometrics;

public sealed class FaceRecognitionService(
    IFaceDetector detector,
    IFaceEmbedder embedder,
    IQualityAssessor qualityAssessor,
    ILivenessDetector livenessDetector,
    IFaceMatcher matcher,
    IBiometricPolicy policy)
    : IFaceRecognitionService
{
    public async Task<FaceEnrollmentResult> EnrollAsync(FaceImagePayload image, CancellationToken ct = default)
    {
        var analysis = await AnalyzeAsync(image, ct);
        return new FaceEnrollmentResult(analysis);
    }

    public async Task<FaceVerificationResult> VerifyAsync(FaceImagePayload image, IReadOnlyList<UserBiometric> references, CancellationToken ct = default)
    {
        if (references.Count == 0)
            throw new FaceRecognitionException("face.no_reference", "No stored facial biometrics available for verification.");

        var analysis = await AnalyzeAsync(image, ct);
        FaceMatchResult? bestMatch = null;
        UserBiometric? bestBiometric = null;

        foreach (var biometric in references.Where(b => b.IsActive))
        {
            var match = matcher.Decide(analysis.Embedding.Embedding, biometric.Embedding, policy.SimilarityMetric, policy.FaceSimilarityThreshold);
            if (bestMatch is null || match.Similarity > bestMatch.Similarity)
            {
                bestMatch = match;
                bestBiometric = biometric;
            }
        }

        if (bestMatch is null || bestBiometric is null)
            throw new FaceRecognitionException("face.no_active_reference", "No active facial biometric matched the provided sample.");

        if (!policy.ValidateMatch(bestMatch, out var reason))
            throw new FaceRecognitionException(reason ?? "face.match_failed", "Face similarity below the required threshold.");

        return new FaceVerificationResult(analysis, bestMatch, bestBiometric);
    }

    private async Task<FaceAnalysis> AnalyzeAsync(FaceImagePayload image, CancellationToken ct)
    {
        if (image.Rgb.Length < image.Width * image.Height * 3)
            throw new FaceRecognitionException("face.image_decode_failed", "Face image payload is inconsistent with declared dimensions.");

        ct.ThrowIfCancellationRequested();
        var detection = await detector.DetectAsync(image.Rgb, image.Width, image.Height, ct);
        var candidate = detection.Faces
            .OrderByDescending(f => f.Confidence)
            .ThenByDescending(f => f.Box.Area)
            .FirstOrDefault();
        if (candidate is null)
            throw new FaceRecognitionException("face.not_found", "No face detected in the provided image.");

        var quality = await qualityAssessor.AssessAsync(image.Rgb, candidate.Box, ct);
        if (!policy.ValidateQuality(quality, out var qualityReason))
            throw new FaceRecognitionException(qualityReason ?? "face.quality_rejected", "Face image quality is insufficient.");

        var liveness = await livenessDetector.EvaluateAsync(image.Rgb, candidate.Box, ct);
        if (!policy.ValidateLiveness(liveness, out var livenessReason))
            throw new FaceRecognitionException(livenessReason ?? "face.liveness_failed", "Liveness verification failed.");

        var embedding = await embedder.EmbedAsync(image.Rgb, candidate.Box, image.Width, image.Height, ct);
        var normalized = matcher.Normalize(embedding.Embedding);
        var normalizedEmbedding = new FaceEmbedding(normalized, embedding.ModelVersion);

        return new FaceAnalysis(candidate, detection, quality, liveness, normalizedEmbedding);
    }
}
