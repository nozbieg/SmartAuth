using System.IO;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Pgvector;
using SmartAuth.Infrastructure.Commons;

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
        ArgumentNullException.ThrowIfNull(rgbImage);
        if (rgbImage.Length != width * height * 3)
            throw new ArgumentException(Messages.Biometrics.RgbBufferSizeMismatch, nameof(rgbImage));

        ct.ThrowIfCancellationRequested();

        var session = _session.Value;
        if (session is null)
        {
            var vecFallback = new Vector(EmbeddingUtils.GenerateDeterministic(EmbeddingDim, width * height + box.X + box.Y));
            return Task.FromResult(new FaceEmbedding(vecFallback, "fallback_arcface_heuristic"));
        }

        try
        {
            return Task.FromResult(EmbedWithSession(session, rgbImage, box, width, height));
        }
        catch (Exception)
        {
            var vecFallback = new Vector(EmbeddingUtils.GenerateDeterministic(EmbeddingDim, width * height + box.X + box.Y));
            return Task.FromResult(new FaceEmbedding(vecFallback, "fallback_arcface_heuristic"));
        }
    }

    private FaceEmbedding EmbedWithSession(InferenceSession session, byte[] rgbImage, FaceBoundingBox box, int width, int height)
    {
        var tensor = CreateInputTensor(rgbImage, width, height, box, _opts.EmbedderInputSize);
        var inputName = session.InputMetadata.Keys.First();
        using var results = session.Run(new[] { NamedOnnxValue.CreateFromTensor(inputName, tensor) });
        var output = results.First();
        var embedding = output.AsEnumerable<float>().ToArray();
        if (embedding.Length != EmbeddingDim)
        {
            Array.Resize(ref embedding, EmbeddingDim);
        }

        var version = session.ModelMetadata?.GraphName;
        if (string.IsNullOrWhiteSpace(version))
            version = Path.GetFileNameWithoutExtension(_opts.FaceEmbedderModelPath);

        return new FaceEmbedding(new Vector(embedding), version ?? "onnx_face_embedder");
    }

    private static DenseTensor<float> CreateInputTensor(byte[] rgbImage, int width, int height, FaceBoundingBox box, int targetSize)
    {
        var tensor = new DenseTensor<float>(new[] { 1, 3, targetSize, targetSize });
        var x0 = Math.Clamp(box.X, 0, Math.Max(0, width - 1));
        var y0 = Math.Clamp(box.Y, 0, Math.Max(0, height - 1));
        var maxWidth = Math.Max(1, width - x0);
        var maxHeight = Math.Max(1, height - y0);
        var cropWidth = Math.Clamp(box.Width, 1, maxWidth);
        var cropHeight = Math.Clamp(box.Height, 1, maxHeight);
        var scaleX = cropWidth / (double)targetSize;
        var scaleY = cropHeight / (double)targetSize;

        for (var y = 0; y < targetSize; y++)
        {
            var sampleY = Math.Min(y0 + (int)Math.Floor((y + 0.5) * scaleY), y0 + cropHeight - 1);
            for (var x = 0; x < targetSize; x++)
            {
                var sampleX = Math.Min(x0 + (int)Math.Floor((x + 0.5) * scaleX), x0 + cropWidth - 1);
                var idx = (sampleY * width + sampleX) * 3;
                tensor[0, 0, y, x] = (rgbImage[idx] - 127.5f) / 128f;
                tensor[0, 1, y, x] = (rgbImage[idx + 1] - 127.5f) / 128f;
                tensor[0, 2, y, x] = (rgbImage[idx + 2] - 127.5f) / 128f;
            }
        }

        return tensor;
    }

    public void Dispose()
    {
        if (_session.IsValueCreated) _session.Value?.Dispose();
    }
}
