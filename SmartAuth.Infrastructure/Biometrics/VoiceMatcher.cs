using Pgvector;
using SmartAuth.Infrastructure.Commons;
using System.Linq;
using System.Reflection;

namespace SmartAuth.Infrastructure.Biometrics;

public sealed class VoiceMatcher : IVoiceMatcher
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
        throw new InvalidOperationException(Messages.Biometrics.PgvectorBufferMissing);
    }

    public double ComputeSimilarity(Vector a, Vector b)
    {
        var av = GetArray(a); var bv = GetArray(b);
        if (av.Length != bv.Length) throw new ArgumentException(Messages.Biometrics.EmbeddingDimensionMismatch);
        double dot = 0, na = 0, nb = 0;
        for (var i = 0; i < av.Length; i++) { dot += av[i] * bv[i]; na += av[i] * av[i]; nb += bv[i] * bv[i]; }
        if (na == 0 || nb == 0) return 0;
        return dot / Math.Sqrt(na * nb);
    }

    public Vector Normalize(Vector v)
    {
        var vals = GetArray(v);
        var norm = Math.Sqrt(vals.Sum(x => x * x));
        if (norm == 0) return v;
        var arr = vals.Select(x => (float)(x / norm)).ToArray();
        return new Vector(arr);
    }

    public VoiceMatchResult Decide(Vector probe, Vector candidate, double threshold)
    {
        var p = Normalize(probe); var c = Normalize(candidate);
        var similarity = ComputeSimilarity(p, c);
        return new VoiceMatchResult(similarity >= threshold, similarity, threshold);
    }
}
