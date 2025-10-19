using Pgvector;
using SmartAuth.Infrastructure.Biometrics;

namespace SmartAuth.Tests.Tests;

public sealed class FaceMatcherTests
{
    private readonly FaceMatcher _matcher = new();

    [Fact]
    public void Cosine_similarity_high_for_identical_vectors()
    {
        var a = new Vector(Enumerable.Range(0, 8).Select(i => (float)i).ToArray());
        var b = new Vector(Enumerable.Range(0, 8).Select(i => (float)i).ToArray());
        var res = _matcher.ComputeSimilarity(a, b, FaceSimilarityMetric.Cosine);
        Assert.True(res > 0.999);
    }

    [Fact]
    public void L2_distance_zero_for_identical_vectors()
    {
        var a = new Vector(new float[]{0,1,2,3});
        var b = new Vector(new float[]{0,1,2,3});
        var dist = _matcher.ComputeSimilarity(a, b, FaceSimilarityMetric.L2);
        Assert.Equal(0, dist, 3);
    }

    [Fact]
    public void Decide_cosine_match_above_threshold()
    {
        var a = new Vector(new float[]{1,2,3,4});
        var b = new Vector(new float[]{1,2,3,4});
        var match = _matcher.Decide(a, b, FaceSimilarityMetric.Cosine, 0.9);
        Assert.True(match.IsMatch);
        Assert.True(match.Similarity >= 0.9);
    }

    [Fact]
    public void Decide_cosine_non_match_below_threshold()
    {
        var a = new Vector(new float[]{1,0,0,0});
        var b = new Vector(new float[]{0,1,0,0});
        var match = _matcher.Decide(a, b, FaceSimilarityMetric.Cosine, 0.5);
        Assert.False(match.IsMatch);
        Assert.True(match.Similarity < 0.5);
    }
}

