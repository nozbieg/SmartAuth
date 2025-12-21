using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SmartAuth.Infrastructure.Biometrics;

public static class BiometricsServiceCollectionExtensions
{
    public static IServiceCollection AddBiometrics(this IServiceCollection services, IConfiguration cfg)
    {
        var opts = new BiometricsOptions();
        var section = cfg.GetSection("Biometrics");
        if (section.Exists()) section.Bind(opts);
        services.AddSingleton(opts);

        services.AddSingleton<IFaceDetector, OnnxFaceDetector>();
        services.AddSingleton<IFaceEmbedder, OnnxFaceEmbedder>();
        services.AddSingleton<ILivenessDetector, PassiveLivenessDetectorV1>();
        services.AddSingleton<IQualityAssessor, QualityAssessorDefault>();
        services.AddSingleton<IFaceMatcher, FaceMatcher>();
        services.AddSingleton<IBiometricPolicy, BiometricPolicyDefault>();
        services.AddSingleton<IFaceRecognitionService, FaceRecognitionService>();
        services.AddSingleton<IVoiceEmbedder, OnnxVoiceEmbedder>();
        services.AddSingleton<IVoiceMatcher, VoiceMatcher>();
        services.AddSingleton<IVoicePolicy, VoicePolicyDefault>();
        services.AddSingleton<IVoiceRecognitionService, VoiceRecognitionService>();
        return services;
    }
}

