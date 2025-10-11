using Pgvector;

namespace SmartAuth.Api.Utilities;

public static class Utility
{
    public static double CosineSimilarity(Vector a, Vector b)
    {
        var av = a.ToArray();
        var bv = b.ToArray();
        if (av.Length != bv.Length) return -1;

        double dot = 0, na = 0, nb = 0;
        for (int i = 0; i < av.Length; i++)
        {
            dot += av[i] * bv[i];
            na += av[i] * av[i];
            nb += bv[i] * bv[i];
        }

        return dot / (Math.Sqrt(na) * Math.Sqrt(nb) + 1e-9);
    }
}