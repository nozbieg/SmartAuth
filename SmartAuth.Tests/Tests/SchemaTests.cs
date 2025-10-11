using Microsoft.EntityFrameworkCore.Metadata;
using Pgvector;
using SmartAuth.Domain.Entities;
using SmartAuth.Tests.Helpers;

namespace SmartAuth.Tests.Tests;

public sealed class SchemaTests(PostgresContainerFixture fx) : IClassFixture<PostgresContainerFixture>
{
    private readonly string _cs = fx.ConnectionString;

    [Fact]
    public async Task Database_is_up_to_date_no_pending_migrations()
    {
        await using var db = DbContextFactory.Create(_cs);

        // Utwórz/aktualizuj schemat zgodnie z migracjami
        await db.Database.MigrateAsync();

        var pending = await db.Database.GetPendingMigrationsAsync();
        Assert.Empty(pending);
    }

    [Fact]
    public async Task Model_entities_tables_columns_indexes_and_relationships_are_configured()
    {
        await using var db = DbContextFactory.Create(_cs);
        await db.Database.MigrateAsync();

        var model = db.Model;

        // === Users ===
        var userEt = model.FindEntityType(typeof(User));
        Assert.NotNull(userEt);
        Assert.NotNull(userEt!.GetTableName());

        var emailProp = userEt.FindProperty(nameof(User.Email));
        Assert.NotNull(emailProp);

        // unikalny indeks na Email (pojedyncza kolumna)
        var emailUniqueIdx = userEt.GetIndexes()
            .FirstOrDefault(i => i.IsUnique && i.Properties.SequenceEqual(new[] { emailProp }));
        Assert.NotNull(emailUniqueIdx);

        // === UserAuthenticators ===
        var uaEt = model.FindEntityType(typeof(UserAuthenticator));
        Assert.NotNull(uaEt);
        Assert.NotNull(uaEt!.GetTableName());

        var uaTypeProp = uaEt.FindProperty(nameof(UserAuthenticator.Type));
        Assert.NotNull(uaTypeProp);

        // relacja UA -> User (wiele do jednego)
        var fkUaUser = uaEt.GetForeignKeys()
            .FirstOrDefault(fk =>
                fk.PrincipalEntityType == userEt && fk.Properties.Any(p => p.Name == nameof(UserAuthenticator.UserId)));
        Assert.NotNull(fkUaUser);

        // === FaceTemplates ===
        var faceEt = model.FindEntityType(typeof(FaceTemplate));
        Assert.NotNull(faceEt);
        Assert.NotNull(faceEt!.GetTableName());

        var faceEmbeddingProp = faceEt.FindProperty(nameof(FaceTemplate.Embedding));
        Assert.NotNull(faceEmbeddingProp);
        Assert.Equal(typeof(Vector), faceEmbeddingProp!.ClrType);

        // Typ kolumny wg konfiguracji (vector(512))
        var faceStore = StoreObjectIdentifier.Table(faceEt.GetTableName()!, faceEt.GetSchema());
        var faceEmbeddingColumnName = faceEmbeddingProp.GetColumnName(faceStore);
        var faceEmbeddingColumnType = faceEmbeddingProp.GetColumnType();
        Assert.False(string.IsNullOrWhiteSpace(faceEmbeddingColumnName));
        Assert.Equal("vector(512)", faceEmbeddingColumnType);

        // relacja FaceTemplate (1:1) -> UserAuthenticator
        var fkFaceUa = faceEt.GetForeignKeys()
            .FirstOrDefault(fk =>
                fk.PrincipalEntityType == uaEt &&
                fk.Properties.Any(p => p.Name == nameof(FaceTemplate.AuthenticatorId)));
        Assert.NotNull(fkFaceUa);
        Assert.True(fkFaceUa!.IsUnique); // 1:1

        // === VoiceTemplates ===
        var voiceEt = model.FindEntityType(typeof(VoiceTemplate));
        Assert.NotNull(voiceEt);
        Assert.NotNull(voiceEt!.GetTableName());

        var voiceEmbeddingProp = voiceEt.FindProperty(nameof(VoiceTemplate.Embedding));
        Assert.NotNull(voiceEmbeddingProp);
        Assert.Equal(typeof(Vector), voiceEmbeddingProp!.ClrType);

        var voiceStore = StoreObjectIdentifier.Table(voiceEt.GetTableName()!, voiceEt.GetSchema());
        var voiceEmbeddingColumnName = voiceEmbeddingProp.GetColumnName(voiceStore);
        var voiceEmbeddingColumnType = voiceEmbeddingProp.GetColumnType();
        Assert.False(string.IsNullOrWhiteSpace(voiceEmbeddingColumnName));
        Assert.Equal("vector(256)", voiceEmbeddingColumnType);

        var fkVoiceUa = voiceEt.GetForeignKeys()
            .FirstOrDefault(fk =>
                fk.PrincipalEntityType == uaEt &&
                fk.Properties.Any(p => p.Name == nameof(VoiceTemplate.AuthenticatorId)));
        Assert.NotNull(fkVoiceUa);
        Assert.True(fkVoiceUa!.IsUnique); // 1:1

        // === AuthAttempts (przykładowa weryfikacja indeksu po CreatedAt) ===
        var attemptEt = model.FindEntityType(typeof(AuthAttempt));
        Assert.NotNull(attemptEt);
        var createdAtProp = attemptEt!.FindProperty(nameof(AuthAttempt.CreatedAt));
        Assert.NotNull(createdAtProp);
        var createdAtIndex = attemptEt.GetIndexes().FirstOrDefault(i => i.Properties.Contains(createdAtProp));
        Assert.NotNull(createdAtIndex);
    }
}