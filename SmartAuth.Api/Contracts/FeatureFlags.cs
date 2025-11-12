namespace SmartAuth.Api.Contracts;

public record FeatureFlags(bool twofa_code, bool twofa_face);

public static class FeatureFlagsConfig
{
    public static bool TwoFaCodeEnabled => true;
    public static bool TwoFaFaceEnabled => true;
}
