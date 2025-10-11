using Pgvector;
using SmartAuth.Api.Utilities;
using SmartAuth.Domain.Entities;
using SmartAuth.Infrastructure.Security;
using SmartAuth.Tests.Helpers;

namespace SmartAuth.Tests.Tests;

public sealed class EmbeddingTests(PostgresContainerFixture fx) : IClassFixture<PostgresContainerFixture>
{
    private readonly string _cs = fx.ConnectionString;

    [Fact]
    public async Task FaceTemplate_roundtrip_and_cosine_similarity()
    {
        await using var db = DbContextFactory.Create(_cs);
        await db.Database.MigrateAsync();

        var (hash, salt) = Passwords.Hash("Passw0rd!");
        var user = new User { Email = "vec@example.com", PasswordHash = hash, PasswordSalt = salt };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var auth = new UserAuthenticator
            { UserId = user.Id, Type = AuthenticatorType.Face, DisplayName = "cam1", IsEnrolled = false };
        db.UserAuthenticators.Add(auth);
        await db.SaveChangesAsync();

        var v512A = new Vector(Enumerable.Range(0, 512).Select(i => i % 10 / 10f).ToArray());
        var v512B = new Vector(Enumerable.Range(0, 512)
            .Select(i => i % 10 / 10f + (i % 2 == 0 ? 0.001f : -0.001f)).ToArray());

        db.FaceTemplates.Add(new FaceTemplate { AuthenticatorId = auth.Id, Embedding = v512A, QualityScore = 0.9f });
        auth.IsEnrolled = true;
        await db.SaveChangesAsync();

        var backEmbedding = await db.FaceTemplates
            .AsNoTracking()
            .Where(x => x.AuthenticatorId == auth.Id)
            .Select(x => x.Embedding)
            .FirstAsync();

        Assert.Equal(v512A.ToArray(), backEmbedding.ToArray());

        var score = Utility.CosineSimilarity(backEmbedding, v512B);
        Assert.True(score > 0.999, $"Cosine too low: {score}");
    }

    [Fact]
    public async Task VoiceTemplate_roundtrip()
    {
        await using var db = DbContextFactory.Create(_cs);
        await db.Database.MigrateAsync();

        var (hash, salt) = Passwords.Hash("Passw0rd!");
        var user = new User { Email = "voice@example.com", PasswordHash = hash, PasswordSalt = salt };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var auth = new UserAuthenticator { UserId = user.Id, Type = AuthenticatorType.Voice, DisplayName = "mic1" };
        db.UserAuthenticators.Add(auth);
        await db.SaveChangesAsync();

        var v256 = new Vector(Enumerable.Range(0, 256).Select(i => (float)Math.Sin(i)).ToArray());

        db.VoiceTemplates.Add(new VoiceTemplate
            { AuthenticatorId = auth.Id, Embedding = v256, Phrase = "open sesame", SampleRate = 16000 });
        await db.SaveChangesAsync();

        
        var voiceEmbedding = await db.VoiceTemplates
            .AsNoTracking()
            .Where(x => x.AuthenticatorId == auth.Id)
            .Select(x => x.Embedding)
            .FirstAsync();

        Assert.Equal(v256.ToArray(), voiceEmbedding.ToArray());
    }
}