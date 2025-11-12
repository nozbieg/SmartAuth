using SmartAuth.Api.Contracts;

namespace SmartAuth.Api.Endpoints;

public static class FeatureFlagEndpoints
{
    public static WebApplication UseFeatureFlagEndpoints(this WebApplication app)
    {
        // Feature Flags (could be served from Commons here)
        app.MapGet("/api/feature-flags",
            (IConfiguration cfg) =>
            {
                var section = cfg.GetSection("FeatureFlags");
                var flags = section.Exists()
                    ? section.Get<FeatureFlags>()
                    : new FeatureFlags(FeatureFlagsConfig.TwoFaCodeEnabled, FeatureFlagsConfig.TwoFaFaceEnabled);
                return Results.Json(flags);
            });
        return app;
    }
}