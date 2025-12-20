using Microsoft.ML.OnnxRuntime;

namespace SmartAuth.Infrastructure.Biometrics;

internal static class OnnxSessionFactory
{
    public static InferenceSession? Create(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        // Sprawdź ścieżkę bezwzględną lub względną
        var resolvedPath = ResolvePath(path);
        if (resolvedPath is null)
            return null;

        return new InferenceSession(resolvedPath, new SessionOptions());
    }

    private static string? ResolvePath(string path)
    {
        // 1. Sprawdź jako ścieżkę bezwzględną lub względną od CWD
        if (File.Exists(path))
            return Path.GetFullPath(path);

        // 2. Sprawdź względem katalogu aplikacji (bin)
        var baseDir = AppContext.BaseDirectory;
        var fromBase = Path.Combine(baseDir, path);
        if (File.Exists(fromBase))
            return fromBase;

        // 3. Sprawdź w katalogu nadrzędnym (dla scenariuszy dev)
        var parent = Directory.GetParent(baseDir)?.FullName;
        while (parent is not null)
        {
            var candidate = Path.Combine(parent, path);
            if (File.Exists(candidate))
                return candidate;

            // Szukaj też w SmartAuth.AppHost/SmartAuth.Infrastructure/models
            var appHostPath = Path.Combine(parent, "SmartAuth.AppHost", "SmartAuth.Infrastructure", path);
            if (File.Exists(appHostPath))
                return appHostPath;

            parent = Directory.GetParent(parent)?.FullName;
        }

        return null;
    }
}
