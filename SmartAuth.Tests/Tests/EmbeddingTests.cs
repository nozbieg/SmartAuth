using Pgvector;
using SmartAuth.Api.Utilities;
using SmartAuth.Domain.Entities;
using SmartAuth.Infrastructure.Security;
using SmartAuth.Tests.Helpers;

namespace SmartAuth.Tests.Tests;

public class EmbeddingTests : IClassFixture<PostgresContainerFixture>
{
    private readonly string _cs;
    public EmbeddingTests(PostgresContainerFixture fx) => _cs = fx.ConnectionString;

    [Fact]
    public async Task FaceTemplate_roundtrip_and_cosine_similarity()
    {
        await using var db = DbContextFactory.Create(_cs);
        await db.Database.MigrateAsync();

        var (hash, salt) = Passwords.Hash("Passw0rd!");
        var user = new User { Email = "vec@example.com", PasswordHash = hash, PasswordSalt = salt };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var auth = new UserAuthenticator { UserId = user.Id, Type = AuthenticatorType.Face, DisplayName = "cam1" };
        db.UserAuthenticators.Add(auth);
        await db.SaveChangesAsync();

        var v = new Vector(new[] { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f });
        db.FaceTemplates.Add(new FaceTemplate { AuthenticatorId = auth.Id, Embedding = v, QualityScore = 0.9f });
        await db.SaveChangesAsync();

        var back = await db.FaceTemplates.AsNoTracking().Include(faceTemplate => faceTemplate.Embedding).FirstAsync();
        Assert.Equal(v.ToArray(), back.Embedding.ToArray());

        var score = Utility.CosineSimilarity(back.Embedding,
            new Vector(new[] { 0.1f, 0.2f, 0.3f, 0.41f, 0.49f, 0.6f, 0.69f, 0.8f }));
        Assert.True(score > 0.99, $"Cosine too low: {score}");
    }
}