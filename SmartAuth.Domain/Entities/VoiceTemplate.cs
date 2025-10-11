using Pgvector;

namespace SmartAuth.Domain.Entities;

public class VoiceTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AuthenticatorId { get; set; }
    public UserAuthenticator Authenticator { get; set; } = default!;
    public required Vector Embedding { get; set; }
    public string Phrase { get; set; } = string.Empty;
    public int SampleRate { get; set; } = 16000;
    public string ModelVersion { get; set; } = "voice-v1";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}