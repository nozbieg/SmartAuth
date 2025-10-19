namespace SmartAuth.Infrastructure.Biometrics;

public sealed class PassiveLivenessDetectorV1 : ILivenessDetector
{
    private readonly BiometricsOptions _opts;
    public PassiveLivenessDetectorV1(BiometricsOptions opts) => _opts = opts;

    public Task<LivenessResult> EvaluateAsync(byte[] rgbImage, FaceBoundingBox box, CancellationToken ct = default)
    {
        var areaRatio = (double)box.Area / Math.Max(1, rgbImage.Length / 3);
        var score = Math.Clamp(areaRatio * 10, 0, 1);
        var pass = score > 0.2;
        return Task.FromResult(new LivenessResult(pass, score, Domain.Entities.LivenessMethod.PassiveV1));
    }
}
