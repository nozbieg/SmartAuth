using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartAuth.Infrastructure.Biometrics;

namespace SmartAuth.Tests.Tests;

public sealed class AddBiometricsExtensionTests
{
    [Fact]
    public void AddBiometrics_registers_services_and_options()
    {
        var dict = new Dictionary<string,string?>
        {
            ["Biometrics:MinOverallQuality"] = "0.55",
            ["Biometrics:SimilarityThresholdCosine"] = "0.5"
        };
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
        var services = new ServiceCollection();
        services.AddBiometrics(cfg);
        var sp = services.BuildServiceProvider();
        var opts = sp.GetService<BiometricsOptions>();
        Assert.NotNull(opts);
        Assert.Equal(0.55, opts!.MinOverallQuality, 3);
        var detector = sp.GetService<IFaceDetector>();
        var embedder = sp.GetService<IFaceEmbedder>();
        var matcher = sp.GetService<IFaceMatcher>();
        var recognition = sp.GetService<IFaceRecognitionService>();
        Assert.NotNull(detector);
        Assert.NotNull(embedder);
        Assert.NotNull(matcher);
        Assert.NotNull(recognition);
    }
}

