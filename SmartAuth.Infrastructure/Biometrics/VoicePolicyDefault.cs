namespace SmartAuth.Infrastructure.Biometrics;

public sealed class VoicePolicyDefault(BiometricsOptions opts) : IVoicePolicy
{
    public double MinDurationSeconds => opts.MinVoiceDuration;
    public double MinEnergyRms => opts.MinVoiceEnergy;
    public double VoiceSimilarityThreshold => opts.VoiceSimilarityThreshold;
    public int MaxRetries => opts.MaxRetries;
    public TimeSpan Cooldown => TimeSpan.FromSeconds(opts.CooldownSeconds);

    public bool ValidateQuality(VoiceQuality metrics, out string? reason)
    {
        if (metrics.DurationSeconds < MinDurationSeconds)
        {
            reason = "voice.too_short";
            return false;
        }

        if (metrics.EnergyRms < MinEnergyRms)
        {
            reason = "voice.too_quiet";
            return false;
        }

        reason = null;
        return true;
    }

    public bool ValidateMatch(VoiceMatchResult match, out string? reason)
    {
        if (!match.IsMatch)
        {
            reason = "voice.match_failed";
            return false;
        }

        reason = null;
        return true;
    }
}
