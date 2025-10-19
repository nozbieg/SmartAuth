using Pgvector;
using System.Reflection;

namespace SmartAuth.Infrastructure.Biometrics;

public sealed class FaceMatcher : IFaceMatcher
{
    private static float[] GetArray(Vector v)
    {
        var t = v.GetType();
        var toArray = t.GetMethod("ToArray", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Array.Empty<Type>());
        if (toArray?.Invoke(v, null) is float[] arr) return arr;
        if (v is IEnumerable<float> enumerable) return enumerable.ToArray();
        var prop = t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault(p => p.PropertyType == typeof(float[]));
        if (prop?.GetValue(v) is float[] arrProp) return arrProp;
        var field = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault(f => f.FieldType == typeof(float[]));
        if (field?.GetValue(v) is float[] arrField) return arrField;
        throw new InvalidOperationException("Brak bufora wektora Pgvector.Vector.");
    }

    public double ComputeSimilarity(Vector a, Vector b, FaceSimilarityMetric metric = FaceSimilarityMetric.Cosine)
    {
        var av = GetArray(a); var bv = GetArray(b);
        if (av.Length != bv.Length) throw new ArgumentException("Embedding dimension mismatch");
        return metric switch { FaceSimilarityMetric.Cosine => Cosine(av, bv), FaceSimilarityMetric.L2 => L2Distance(av, bv), _ => throw new NotSupportedException() };
    }

    public Vector Normalize(Vector v)
    {
        var vals = GetArray(v); var norm = Math.Sqrt(vals.Sum(x => x * x));
        if (norm == 0) return v;
        var arr = vals.Select(x => (float)(x / norm)).ToArray();
        return new Vector(arr);
    }

    public FaceMatchResult Decide(Vector probe, Vector candidate, FaceSimilarityMetric metric, double threshold)
    {
        var p = Normalize(probe); var c = Normalize(candidate);
        var sim = ComputeSimilarity(p, c, metric);
        var isMatch = metric == FaceSimilarityMetric.Cosine ? sim >= threshold : sim <= threshold;
        return new FaceMatchResult(isMatch, sim, metric, threshold);
    }

    private static double Cosine(IReadOnlyList<float> a, IReadOnlyList<float> b)
    {
        double dot = 0, na = 0, nb = 0;
        for (int i = 0; i < a.Count; i++) { dot += a[i] * b[i]; na += a[i] * a[i]; nb += b[i] * b[i]; }
        var denom = Math.Sqrt(na) * Math.Sqrt(nb);
        return denom == 0 ? 0 : dot / denom;
    }

    private static double L2Distance(IReadOnlyList<float> a, IReadOnlyList<float> b)
    {
        double sum = 0; for (int i = 0; i < a.Count; i++) { var d = a[i] - b[i]; sum += d * d; }
        return Math.Sqrt(sum);
    }
}
