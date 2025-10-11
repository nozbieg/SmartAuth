using Pgvector;

namespace SmartAuth.Api.Utilities;

public static class VectorMapping
{
    public static Vector ToVector(this float[] source) => new Vector(source);
}