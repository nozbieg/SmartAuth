namespace SmartAuth.Api.Contracts;

public record FeatureFlags(bool twofa_code);

public static class FeatureFlagsConfig
{
    public static bool TwoFaCodeEnabled => true;
}