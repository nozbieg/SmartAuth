using SmartAuth.Domain.Common;
using Pgvector;

namespace SmartAuth.Domain.Entities;

public class UserBiometric : AuditableEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    // Powiązane z AuthenticatorType (Face)
    public AuthenticatorType Kind { get; set; } = AuthenticatorType.Face;
    // Embedding wektorowy (np. 512 floatów) do wyszukiwania w pgvector
    public Vector Embedding { get; set; } = null!; // ustawiany przy tworzeniu; wymagany (vector(512))
    // Wersja modelu / pipeline'u np. "arcface_1.0"
    public string Version { get; set; } = string.Empty;
    // Ocena jakości zdjęcia / embeddingu (0-1 lub inna skala)
    public double QualityScore { get; set; }
    public LivenessMethod LivenessMethod { get; set; } = LivenessMethod.PassiveV1;
    public bool IsActive { get; set; } = true;

    // Parametry nagrania audio (dla biometrii głosu)
    public int? AudioSampleRate { get; set; }
    public double? AudioDurationSeconds { get; set; }
}

public enum LivenessMethod { PassiveV1 = 1, ActiveChallenge = 2 }
