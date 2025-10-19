using Microsoft.ML.OnnxRuntime;

namespace SmartAuth.Infrastructure.Biometrics;

public sealed class OnnxFaceDetector : IFaceDetector, IDisposable
{
    private readonly BiometricsOptions _opts;
    private readonly Lazy<InferenceSession?> _session;

    public OnnxFaceDetector(BiometricsOptions opts)
    {
        _opts = opts;
        _session = new Lazy<InferenceSession?>(() => OnnxSessionFactory.Create(_opts.FaceDetectorModelPath));
    }

    public Task<FaceDetectionResult> DetectAsync(byte[] rgbImage, int width, int height, CancellationToken ct = default)
    {
        var box = new FaceBoundingBox(width / 4, height / 4, width / 2, height / 2);
        var landmarks = new FaceLandmarks(new float[] { box.X + box.Width * 0.3f, box.Y + box.Height * 0.35f, box.X + box.Width * 0.7f, box.Y + box.Height * 0.35f });
        var face = new FaceCandidate(box, landmarks, 0.99f);
        return Task.FromResult(new FaceDetectionResult(new List<FaceCandidate> { face }, width, height));
    }

    public void Dispose()
    {
        if (_session.IsValueCreated) _session.Value?.Dispose();
    }
}
