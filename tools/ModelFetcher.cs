using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmartAuth.AppHost.Tools;

public static class ModelFetcher
{
    private record ModelConfig(string Key, string FileName, string? Url);

    private record ScriptModelEntry(string Name, string Url, string FileName);

    public static void TryFetchModelsIfNeeded(IConfiguration? configuration)
    {
        try
        {
            var root = configuration?.GetSection("ModelFetching");
            if (root is null)
            {
                Console.WriteLine("[models] No ModelFetching section found; skipping.");
                return;
            }

            var skip = root.GetValue<bool>("Skip");
            if (skip)
            {
                Console.WriteLine("[models] Skipped (ModelFetching.Skip=true).");
                return;
            }

            var verbose = root.GetValue<bool?>("Verbose") ?? true;
            var directorySetting = root.GetValue<string?>("Directory")?.Trim();
            var targetDir = ResolveDirectory(directorySetting);
            if (targetDir is null)
            {
                Console.WriteLine("[models] Cannot resolve target directory. Set ModelFetching:Directory.");
                return;
            }

            Directory.CreateDirectory(targetDir);

            var modelsSection = root.GetSection("Models");
            if (!modelsSection.Exists())
            {
                Console.WriteLine("[models] No ModelFetching.Models section defined; nothing to fetch.");
                return;
            }

            var modelConfigs = new List<ModelConfig>();
            foreach (var child in modelsSection.GetChildren())
            {
                var fileName = child.GetValue<string>("FileName");
                var url = child.GetValue<string?>("Url");
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    Console.WriteLine($"[models] Model '{child.Key}' missing FileName; skipping.");
                    continue;
                }

                modelConfigs.Add(new ModelConfig(child.Key, fileName, url));
            }

            if (modelConfigs.Count == 0)
            {
                Console.WriteLine("[models] No valid model entries found.");
                return;
            }

            var missing = new List<ScriptModelEntry>();
            foreach (var mc in modelConfigs)
            {
                var path = Path.Combine(targetDir, mc.FileName);
                if (File.Exists(path))
                {
                    if (verbose) Console.WriteLine($"[models] Present: {mc.FileName}");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(mc.Url))
                {
                    Console.WriteLine(
                        $"[models] No URL for missing model '{mc.Key}' ({mc.FileName}); skip downloading.");
                    continue;
                }

                missing.Add(new ScriptModelEntry(mc.Key, mc.Url!, mc.FileName));
            }

            if (missing.Count == 0)
            {
                Console.WriteLine("[models] All configured models already exist or lack URLs; nothing to fetch.");
                return;
            }

            var specJson = JsonSerializer.Serialize(missing, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            var scriptPath = ExtractEmbeddedScript();
            if (scriptPath is null)
            {
                Console.WriteLine("[models] Embedded script not found; aborting fetch.");
                return;
            }

            string psExe = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "powershell" : "pwsh";
            var startInfo = new ProcessStartInfo
            {
                FileName = psExe,
                Arguments = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? $"-ExecutionPolicy Bypass -File \"{scriptPath}\""
                    : $"-File \"{scriptPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Environment.CurrentDirectory
            };
            startInfo.Environment["MODELS_TARGET_DIR"] = targetDir;
            startInfo.Environment["MODELS_SPEC"] = specJson;
            startInfo.Environment["MODEL_FETCH_VERBOSE"] = verbose ? "true" : "false";

            Console.WriteLine(
                $"[models] Fetching {missing.Count} model(s): {string.Join(", ", missing.Select(m => m.FileName))} -> {targetDir}");
            using var proc = Process.Start(startInfo);
            if (proc == null)
            {
                Console.WriteLine("[models] Failed to start PowerShell process.");
                return;
            }

            proc.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null) Console.WriteLine(e.Data);
            };
            proc.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null) Console.Error.WriteLine(e.Data);
            };
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            proc.WaitForExit();
            Console.WriteLine($"[models] Script exited with code {proc.ExitCode}.");
            try
            {
                File.Delete(scriptPath);
            }
            catch
            {
                /* ignore */
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[models] ERROR: " + ex.Message);
        }
    }

    private static string? ResolveDirectory(string? configured)
    {
        if (string.IsNullOrWhiteSpace(configured)) return null;
        return Path.IsPathRooted(configured)
            ? configured
            : Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, configured));
    }

    private static string? ExtractEmbeddedScript()
    {
        var asm = Assembly.GetExecutingAssembly();
        var resName = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("fetch-models.ps1", StringComparison.OrdinalIgnoreCase));
        if (resName is null) return null;
        var tempScriptPath = Path.Combine(Path.GetTempPath(), "fetch-models_" + Guid.NewGuid() + ".ps1");
        using var stream = asm.GetManifestResourceStream(resName);
        if (stream is null) return null;
        using var fs = File.Create(tempScriptPath);
        stream.CopyTo(fs);
        return tempScriptPath;
    }
}