using SmartAuth.Tests.Helpers;

namespace SmartAuth.Tests.Tests;

public sealed class VectorIndexTests : IClassFixture<PostgresContainerFixture>
{
    private readonly string _cs;
    public VectorIndexTests(PostgresContainerFixture fx) => _cs = fx.ConnectionString;

    [Fact]
    public async Task Ivf_indexes_exist_when_pgvector_is_available()
    {
        await using var db = DbContextFactory.Create(_cs);
        await db.Database.MigrateAsync();

        var faceIdxExists = await db.Database.SqlQueryRaw<int>(
                "SELECT 1 FROM pg_indexes WHERE tablename='face_templates' AND indexname='idx_face_embedding_ivf'")
            .AnyAsync();
        var voiceIdxExists = await db.Database.SqlQueryRaw<int>(
                "SELECT 1 FROM pg_indexes WHERE tablename='voice_templates' AND indexname='idx_voice_embedding_ivf'")
            .AnyAsync();

        // Jeśli masz osobną migrację pod IVF, oczekujemy true:
        Assert.True(faceIdxExists, "idx_face_embedding_ivf should exist");
        Assert.True(voiceIdxExists, "idx_voice_embedding_ivf should exist");
    }
}