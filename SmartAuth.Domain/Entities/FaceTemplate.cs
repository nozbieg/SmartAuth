using Pgvector;
using SmartAuth.Domain.Common;

namespace SmartAuth.Domain.Entities;

public class FaceTemplate : AuditableEntity
{
    public Guid AuthenticatorId { get; set; }
    public UserAuthenticator Authenticator { get; set; } = default!;
    public required Vector Embedding { get; set; }
    public string ModelVersion { get; set; } = "face-v1";
    public float LivenessThreshold { get; set; } = 0.8f;
    public float QualityScore { get; set; }
}