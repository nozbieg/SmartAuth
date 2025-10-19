namespace SmartAuth.Infrastructure.Biometrics;

public sealed class BiometricPolicyDefault : IBiometricPolicy
{
    private readonly BiometricsOptions _opts;
    public BiometricPolicyDefault(BiometricsOptions opts) => _opts = opts;

    public double MinQualityOverall => _opts.MinOverallQuality;
    public double FaceSimilarityThreshold => _opts.SimilarityThresholdCosine;
    public FaceSimilarityMetric SimilarityMetric => FaceSimilarityMetric.Cosine;
    public int MaxRetries => _opts.MaxRetries;
    public TimeSpan Cooldown => TimeSpan.FromSeconds(_opts.CooldownSeconds);

    public bool ValidateQuality(QualityMetrics metrics, out string? reason)
    {
        if (!metrics.ResolutionOk) { reason = "resolution_insufficient"; return false; }
        if (!metrics.IsAcceptable(MinQualityOverall)) { reason = "quality_below_threshold"; return false; }
        reason = null; return true;
    }

    public bool ValidateLiveness(LivenessResult liveness, out string? reason)
    {
        if (!liveness.Pass) { reason = "liveness_failed"; return false; }
        reason = null; return true;
    }

    public bool ValidateMatch(FaceMatchResult match, out string? reason)
    {
        if (!match.IsMatch) { reason = "similarity_below_threshold"; return false; }
        reason = null; return true;
    }
}
