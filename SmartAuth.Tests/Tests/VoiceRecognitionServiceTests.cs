using Pgvector;
using SmartAuth.Domain.Entities;
using SmartAuth.Infrastructure.Biometrics;

namespace SmartAuth.Tests.Tests;

public sealed class VoiceRecognitionServiceTests
{
    private sealed class FakeVoiceEmbedder(Vector vector) : IVoiceEmbedder
    {
        public Task<VoiceEmbedding> EmbedAsync(VoiceSamplePayload sample, CancellationToken ct = default)
        {
            return Task.FromResult(new VoiceEmbedding(vector, "fake_voice"));
        }
    }

    [Fact]
    public async Task VerifyAsync_returns_match_for_similar_embeddings()
    {
        var opts = new BiometricsOptions { MinVoiceDuration = 1.0, MinVoiceEnergy = 0.01, VoiceSimilarityThreshold = 0.5 };
        var vector = new Vector(Enumerable.Repeat(0.5f, opts.VoiceEmbeddingDimension).ToArray());
        var embedder = new FakeVoiceEmbedder(vector);
        var matcher = new VoiceMatcher();
        var policy = new VoicePolicyDefault(opts);
        var service = new VoiceRecognitionService(embedder, matcher, policy);
        var payload = new VoiceSamplePayload(opts.VoiceSampleRate, 1, Enumerable.Repeat(0.1f, opts.VoiceSampleRate * 2).ToArray());
        var reference = new UserBiometric
        {
            Kind = AuthenticatorType.Voice,
            Embedding = vector,
            IsActive = true
        };

        var result = await service.VerifyAsync(payload, new List<UserBiometric> { reference });

        Assert.True(result.Match.IsMatch);
        Assert.Equal(reference, result.MatchedBiometric);
        Assert.True(result.Quality.DurationSeconds >= 1);
    }

    [Fact]
    public async Task VerifyAsync_throws_when_duration_too_short()
    {
        var opts = new BiometricsOptions { MinVoiceDuration = 1.5, MinVoiceEnergy = 0.01, VoiceSimilarityThreshold = 0.5 };
        var vector = new Vector(Enumerable.Repeat(0.2f, opts.VoiceEmbeddingDimension).ToArray());
        var service = new VoiceRecognitionService(new FakeVoiceEmbedder(vector), new VoiceMatcher(), new VoicePolicyDefault(opts));
        var payload = new VoiceSamplePayload(opts.VoiceSampleRate, 1, Enumerable.Repeat(0.05f, 1000).ToArray());
        var reference = new UserBiometric { Kind = AuthenticatorType.Voice, Embedding = vector, IsActive = true };

        var ex = await Assert.ThrowsAsync<VoiceRecognitionException>(() => service.VerifyAsync(payload, new List<UserBiometric> { reference }));
        Assert.Equal("voice.too_short", ex.Code);
    }
}
