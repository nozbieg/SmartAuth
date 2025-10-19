using Pgvector;
using SmartAuth.Domain.Entities;

namespace SmartAuth.Infrastructure.Biometrics;

public enum FaceSimilarityMetric { Cosine = 1, L2 = 2 }

public sealed record FaceBoundingBox(int X, int Y, int Width, int Height)
{
    public int Right => X + Width;
    public int Bottom => Y + Height;
    public int Area => Width * Height;
}

public sealed record FaceLandmarks(float[] Points); // e.g. [x1,y1,x2,y2,...]

public sealed record FaceCandidate(FaceBoundingBox Box, FaceLandmarks Landmarks, float Confidence);

public sealed record FaceDetectionResult(IReadOnlyList<FaceCandidate> Faces, int ImageWidth, int ImageHeight);

public sealed record FaceEmbedding(Vector Embedding, string ModelVersion);

public sealed record LivenessResult(bool Pass, double Score, LivenessMethod Method, string? Detail = null);

public sealed record QualityMetrics(double Sharpness, double Lighting, double Frontality, bool ResolutionOk, double Overall)
{
    public bool IsAcceptable(double minOverall) => Overall >= minOverall;
}

public sealed record FaceMatchResult(bool IsMatch, double Similarity, FaceSimilarityMetric Metric, double Threshold);

public interface IFaceDetector
{
    Task<FaceDetectionResult> DetectAsync(byte[] rgbImage, int width, int height, CancellationToken ct = default);
}

public interface IFaceEmbedder
{
    Task<FaceEmbedding> EmbedAsync(byte[] rgbImage, FaceBoundingBox box, int width, int height, CancellationToken ct = default);
}

public interface ILivenessDetector
{
    Task<LivenessResult> EvaluateAsync(byte[] rgbImage, FaceBoundingBox box, CancellationToken ct = default);
}

public interface IQualityAssessor
{
    Task<QualityMetrics> AssessAsync(byte[] rgbImage, FaceBoundingBox box, CancellationToken ct = default);
}

public interface IFaceMatcher
{
    double ComputeSimilarity(Vector a, Vector b, FaceSimilarityMetric metric = FaceSimilarityMetric.Cosine);
    Vector Normalize(Vector v);
    FaceMatchResult Decide(Vector probe, Vector candidate, FaceSimilarityMetric metric, double threshold);
}

public interface IBiometricPolicy
{
    double MinQualityOverall { get; }
    double FaceSimilarityThreshold { get; }
    FaceSimilarityMetric SimilarityMetric { get; }
    int MaxRetries { get; }
    TimeSpan Cooldown { get; }
    bool ValidateQuality(QualityMetrics metrics, out string? reason);
    bool ValidateLiveness(LivenessResult liveness, out string? reason);
    bool ValidateMatch(FaceMatchResult match, out string? reason);
}
