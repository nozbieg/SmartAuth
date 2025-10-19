using System.Security.Cryptography;

namespace SmartAuth.Infrastructure.Biometrics;

internal static class EmbeddingUtils
{
    public static float[] GenerateDeterministic(int dim, int seed)
    {
        var rng = new Random(seed);
        var arr = new float[dim];
        for (int i = 0; i < dim; i++) arr[i] = (float)rng.NextDouble();
        return arr;
    }
}
