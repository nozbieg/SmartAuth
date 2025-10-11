using Pgvector;

namespace SmartAuth.Domain.Entities;

public class FaceTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AuthenticatorId { get; set; }
    public UserAuthenticator Authenticator { get; set; } = default!;
    public required Vector Embedding { get; set; }
    public string ModelVersion { get; set; } = "face-v1";
    public float LivenessThreshold { get; set; } = 0.8f;
    public float QualityScore { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}