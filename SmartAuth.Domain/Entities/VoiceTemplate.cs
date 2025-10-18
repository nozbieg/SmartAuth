using Pgvector;
using SmartAuth.Domain.Common;

namespace SmartAuth.Domain.Entities;

public class VoiceTemplate : AuditableEntity
{
    public Guid AuthenticatorId { get; set; }
    public UserAuthenticator Authenticator { get; set; } = default!;
    public required Vector Embedding { get; set; }
    public string Phrase { get; set; } = string.Empty;
    public int SampleRate { get; set; } = 16000;
    public string ModelVersion { get; set; } = "voice-v1";
}