namespace SmartAuth.Api.Contracts;

public record FeatureFlags(bool twofa_code, bool twofa_face, bool twofa_voice);