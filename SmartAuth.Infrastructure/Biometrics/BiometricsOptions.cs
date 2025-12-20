namespace SmartAuth.Infrastructure.Biometrics;

public sealed class BiometricsOptions
{
    // Ścieżki modeli ONNX (konfiguracja w appsettings lub secret store)
    public string FaceDetectorModelPath { get; set; } = "models/retinaface.onnx";
    public string FaceEmbedderModelPath { get; set; } = "models/arcface.onnx";
    public string PassiveLivenessModelPath { get; set; } = "models/liveness_passive_v1.onnx";

    // Wymiary wejściowe sieci embeddingu / detekcji
    public int EmbedderInputSize { get; set; } = 112; // arcface-like
    public int DetectorInputSize { get; set; } = 640; // retinaface typical

    // Polityka jakości / podobieństwa
    public double MinOverallQuality { get; set; } = 0.70;      
    public double LightingWeight { get; set; } = 0.30;          
    public double SharpnessWeight { get; set; } = 0.35;          
    public double FrontalityWeight { get; set; } = 0.35;      
    public double SimilarityThresholdCosine { get; set; } = 0.55;
    public int MaxRetries { get; set; } = 3;
    public int CooldownSeconds { get; set; } = 10;
}

