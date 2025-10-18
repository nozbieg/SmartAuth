using SmartAuth.Api.Contracts;

namespace SmartAuth.Api.Endpoints;

public static class FeatureFlagEndpoints
{
    public static WebApplication UseFeatureFlagEndpoints(this WebApplication app)
    {
        // Feature Flags (could be served from Commons here)
        app.MapGet("/api/feature-flags",
            (IConfiguration cfg) => Results.Json(new FeatureFlags(FeatureFlagsConfig.TwoFaCodeEnabled)));
        return app;
    }
}