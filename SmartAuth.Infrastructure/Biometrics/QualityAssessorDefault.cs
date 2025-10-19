namespace SmartAuth.Infrastructure.Biometrics;

public sealed class QualityAssessorDefault : IQualityAssessor
{
    private readonly BiometricsOptions _opts;
    public QualityAssessorDefault(BiometricsOptions opts) => _opts = opts;

    private QualityMetrics Compute(int area, int bytes, FaceBoundingBox box)
    {
        var resolutionOk = box.Width >= _opts.EmbedderInputSize && box.Height >= _opts.EmbedderInputSize;
        var sharpness = Math.Clamp(area / (double)Math.Max(1, bytes / 3) * 10, 0, 1);
        const double lighting = 0.7;
        const double frontal = 0.8;
        var overall = sharpness * _opts.SharpnessWeight + lighting * _opts.LightingWeight + frontal * _opts.FrontalityWeight;
        return new QualityMetrics(sharpness, lighting, frontal, resolutionOk, overall);
    }

    public Task<QualityMetrics> AssessAsync(byte[] rgbImage, FaceBoundingBox box, CancellationToken ct = default)
    {
        var metrics = Compute(box.Area, rgbImage.Length, box);
        return Task.FromResult(metrics);
    }
}
