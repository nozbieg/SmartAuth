using System.IO;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Pgvector;
using SmartAuth.Infrastructure.Commons;

namespace SmartAuth.Infrastructure.Biometrics;

public sealed class OnnxVoiceEmbedder : IVoiceEmbedder, IDisposable
{
    private readonly BiometricsOptions _opts;
    private readonly Lazy<InferenceSession?> _session;

    public OnnxVoiceEmbedder(BiometricsOptions opts)
    {
        _opts = opts;
        _session = new Lazy<InferenceSession?>(() => OnnxSessionFactory.Create(_opts.VoiceEmbedderModelPath));
    }

    public Task<VoiceEmbedding> EmbedAsync(VoiceSamplePayload sample, CancellationToken ct = default)
    {
        if (sample.Samples.Length == 0)
            throw new ArgumentException(Messages.Biometrics.AudioRequired, nameof(sample));

        ct.ThrowIfCancellationRequested();

        var session = _session.Value;
        if (session is null)
        {
            var seed = (int)(sample.DurationSeconds * 1000) + sample.Samples.Length;
            var vecFallback = new Vector(EmbeddingUtils.GenerateDeterministic(_opts.VoiceEmbeddingDimension, seed));
            return Task.FromResult(new VoiceEmbedding(vecFallback, "fallback_voice_heuristic"));
        }

        try
        {
            return Task.FromResult(EmbedWithSession(session, sample));
        }
        catch (Exception)
        {
            var seed = (int)(sample.DurationSeconds * 1000) + sample.Samples.Length;
            var vecFallback = new Vector(EmbeddingUtils.GenerateDeterministic(_opts.VoiceEmbeddingDimension, seed));
            return Task.FromResult(new VoiceEmbedding(vecFallback, "fallback_voice_heuristic"));
        }
    }

    private VoiceEmbedding EmbedWithSession(InferenceSession session, VoiceSamplePayload sample)
    {
        var maxSamples = Math.Max(1, _opts.VoiceSampleRate * _opts.VoiceMaxDurationSeconds);
        var trimmed = sample.Samples.Length > maxSamples
            ? sample.Samples.Take(maxSamples).ToArray()
            : sample.Samples;
        var normalized = NormalizeAudio(trimmed, sample.Channels);
        var inputName = session.InputMetadata.Keys.First();
        var tensor = CreateInputTensor(normalized);
        using var results = session.Run(new[] { NamedOnnxValue.CreateFromTensor(inputName, tensor) });
        var output = results.First();
        var embedding = output.AsEnumerable<float>().ToArray();
        if (embedding.Length != _opts.VoiceEmbeddingDimension)
        {
            Array.Resize(ref embedding, _opts.VoiceEmbeddingDimension);
        }

        var version = session.ModelMetadata?.GraphName;
        if (string.IsNullOrWhiteSpace(version))
            version = Path.GetFileNameWithoutExtension(_opts.VoiceEmbedderModelPath);

        return new VoiceEmbedding(new Vector(embedding), version ?? "onnx_voice_embedder");
    }

    private static DenseTensor<float> CreateInputTensor(float[] samples)
    {
        var tensor = new DenseTensor<float>(new[] { 1, samples.Length });
        for (var i = 0; i < samples.Length; i++) tensor[0, i] = samples[i];
        return tensor;
    }

    private static float[] NormalizeAudio(float[] samples, int channels)
    {
        if (channels <= 1) return samples;
        var mono = new float[samples.Length / channels];
        for (var i = 0; i < mono.Length; i++)
        {
            var sum = 0f;
            for (var ch = 0; ch < channels; ch++)
            {
                sum += samples[i * channels + ch];
            }
            mono[i] = sum / channels;
        }
        return mono;
    }

    public void Dispose()
    {
        if (_session.IsValueCreated) _session.Value?.Dispose();
    }
}
