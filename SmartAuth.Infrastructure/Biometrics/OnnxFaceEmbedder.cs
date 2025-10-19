using Microsoft.ML.OnnxRuntime;
using Pgvector;

namespace SmartAuth.Infrastructure.Biometrics;

public sealed class OnnxFaceEmbedder : IFaceEmbedder, IDisposable
{
    private readonly BiometricsOptions _opts;
    private readonly Lazy<InferenceSession?> _session;
    private const int EmbeddingDim = 512;

    public OnnxFaceEmbedder(BiometricsOptions opts)
    {
        _opts = opts;
        _session = new Lazy<InferenceSession?>(() => OnnxSessionFactory.Create(_opts.FaceEmbedderModelPath));
    }

    public Task<FaceEmbedding> EmbedAsync(byte[] rgbImage, FaceBoundingBox box, int width, int height, CancellationToken ct = default)
    {
        var vec = new Vector(EmbeddingUtils.GenerateDeterministic(EmbeddingDim, width * height + box.X + box.Y));
        return Task.FromResult(new FaceEmbedding(vec, "arcface_stub_1.0"));
    }

    public void Dispose()
    {
        if (_session.IsValueCreated) _session.Value?.Dispose();
    }
}
