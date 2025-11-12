using SmartAuth.Domain.Entities;

namespace SmartAuth.Infrastructure.Biometrics;

public sealed record FaceAnalysis(
    FaceCandidate Candidate,
    FaceDetectionResult Detection,
    QualityMetrics Quality,
    LivenessResult Liveness,
    FaceEmbedding Embedding);

public sealed record FaceEnrollmentResult(FaceAnalysis Analysis);

public sealed record FaceVerificationResult(FaceAnalysis Analysis, FaceMatchResult Match, UserBiometric MatchedBiometric);

public interface IFaceRecognitionService
{
    Task<FaceEnrollmentResult> EnrollAsync(FaceImagePayload image, CancellationToken ct = default);
    Task<FaceVerificationResult> VerifyAsync(FaceImagePayload image, IReadOnlyList<UserBiometric> references, CancellationToken ct = default);
}
