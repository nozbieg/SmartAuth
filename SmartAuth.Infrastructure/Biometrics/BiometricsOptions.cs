namespace SmartAuth.Infrastructure.Biometrics;

public sealed class BiometricsOptions
{
    // Ścieżki modeli ONNX (konfiguracja w appsettings lub secret store)
    public string FaceDetectorModelPath { get; set; } = "models/ultraface.onnx";
    public string FaceEmbedderModelPath { get; set; } = "models/arcface.onnx";
    public string PassiveLivenessModelPath { get; set; } = "models/liveness_passive_v1.onnx";
    public string VoiceEmbedderModelPath { get; set; } = "models/voice_ecapa.onnx";

    // Wymiary wejściowe sieci embeddingu / detekcji
    public int EmbedderInputSize { get; set; } = 112; // arcface-like
    public int DetectorInputSize { get; set; } = 640; // retinaface typical
    public int VoiceEmbeddingDimension { get; set; } = 512;
    public int VoiceSampleRate { get; set; } = 16000;
    public int VoiceMaxDurationSeconds { get; set; } = 6;

    // Polityka jakości / podobieństwa
    public double MinOverallQuality { get; set; } = 0.70;
    public double LightingWeight { get; set; } = 0.30;
    public double SharpnessWeight { get; set; } = 0.35;
    public double FrontalityWeight { get; set; } = 0.35;
    public double SimilarityThresholdCosine { get; set; } = 0.55;
    public double VoiceSimilarityThreshold { get; set; } = 0.75;
    public double MinVoiceDuration { get; set; } = 2.2;
    public double MinVoiceEnergy { get; set; } = 0.015;
    public int MaxRetries { get; set; } = 3;
    public int CooldownSeconds { get; set; } = 10;
}

