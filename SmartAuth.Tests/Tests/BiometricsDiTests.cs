using SmartAuth.Infrastructure.Biometrics;
using SmartAuth.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace SmartAuth.Tests.Tests;

public sealed class BiometricsDiTests(PostgresContainerFixture fx) : IClassFixture<PostgresContainerFixture>
{
    [Fact]
    public void Can_resolve_biometric_services()
    {
        var cs = fx.ConnectionString;
        var (provider, mediator) = TestSetup.BuildMediator(cs, twoFaCodeEnabled: false, overrides: new Dictionary<string,string?>
        {
            ["Biometrics:MinOverallQuality"] = "0.5",
            ["Biometrics:SimilarityThresholdCosine"] = "0.4"
        });
        var detector = provider.GetRequiredService<IFaceDetector>();
        var embedder = provider.GetRequiredService<IFaceEmbedder>();
        var liveness = provider.GetRequiredService<ILivenessDetector>();
        var quality = provider.GetRequiredService<IQualityAssessor>();
        var matcher = provider.GetRequiredService<IFaceMatcher>();
        var policy = provider.GetRequiredService<IBiometricPolicy>();
        Assert.True(policy.MinQualityOverall <= 0.5);
    }
}
