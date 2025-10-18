using Pgvector;
using SmartAuth.Api.Utilities;

namespace SmartAuth.Tests.Tests;

public class CosineSimilarityTests
{
    [Fact]
    public void Identical_vectors_similarity_is_one()
    {
        var v1 = new Vector(new float[]{1f,2f,3f});
        var v2 = new Vector(new float[]{1f,2f,3f});
        var sim = Utility.CosineSimilarity(v1, v2);
        Assert.True(sim > 0.999); // tolerancja
    }

    [Fact]
    public void Orthogonal_vectors_similarity_is_near_zero()
    {
        var v1 = new Vector(new float[]{1f,0f});
        var v2 = new Vector(new float[]{0f,1f});
        var sim = Utility.CosineSimilarity(v1, v2);
        Assert.True(sim < 0.01);
    }

    [Fact]
    public void Mismatched_dimensions_returns_minus_one()
    {
        var v1 = new Vector(new float[]{1f});
        var v2 = new Vector(new float[]{1f,2f});
        Assert.Equal(-1, Utility.CosineSimilarity(v1, v2));
    }

    [Fact]
    public void Zero_vectors_similarity_is_zero()
    {
        var v1 = new Vector(new float[]{0f,0f,0f});
        var v2 = new Vector(new float[]{0f,0f,0f});
        var sim = Utility.CosineSimilarity(v1, v2);
        Assert.Equal(0, sim, 3);
    }
}
