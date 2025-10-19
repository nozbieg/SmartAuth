using Microsoft.ML.OnnxRuntime;

namespace SmartAuth.Infrastructure.Biometrics;

internal static class OnnxSessionFactory
{
    public static InferenceSession? Create(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;
        return new InferenceSession(path, new SessionOptions());
    }
}
