using Microsoft.EntityFrameworkCore;
using Pgvector;
using SmartAuth.Domain.Entities;
using SmartAuth.Tests.Helpers;

namespace SmartAuth.Tests.Tests;

public sealed class UserBiometricTests(PostgresContainerFixture fx) : IClassFixture<PostgresContainerFixture>
{
    private readonly string _cs = fx.ConnectionString;

    private static Vector CreateVector(int dim) => new Vector(Enumerable.Range(0, dim).Select(i => (float)(i % 10) / 10f).ToArray());

    [Fact]
    public async Task Can_add_face_biometric_embedding_for_user()
    {
        await using var db = DbContextFactory.Create(_cs);
        await db.Database.MigrateAsync();

        var (hash, salt) = Infrastructure.Security.Passwords.Hash("Passw0rd!");
        var user = new User { Email = "face1@example.com", PasswordHash = hash, PasswordSalt = salt };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var embedding = CreateVector(512);
        var bio = new UserBiometric
        {
            UserId = user.Id,
            Kind = AuthenticatorType.Face,
            Embedding = embedding,
            Version = "arcface_1.0",
            QualityScore = 0.823,
            LivenessMethod = LivenessMethod.PassiveV1,
            IsActive = true
        };
        db.UserBiometrics.Add(bio);
        await db.SaveChangesAsync();

        Assert.NotEqual(Guid.Empty, bio.Id);
        Assert.NotNull(bio.Embedding);
        Assert.True(bio.IsActive);
        Assert.Single(db.UserBiometrics.Where(b => b.UserId == user.Id));
    }

    [Fact]
    public async Task Model_contains_user_biometrics_entity_with_vector_column()
    {
        await using var db = DbContextFactory.Create(_cs);
        await db.Database.MigrateAsync();
        var et = db.Model.FindEntityType(typeof(UserBiometric));
        Assert.NotNull(et);
        Assert.Equal("user_biometrics", et.GetTableName());
        var embProp = et.FindProperty(nameof(UserBiometric.Embedding));
        Assert.NotNull(embProp);
        Assert.Equal("vector(512)", embProp.GetColumnType());
    }

    [Fact]
    public async Task Deleting_user_cascades_deleting_biometrics()
    {
        await using var db = DbContextFactory.Create(_cs);
        await db.Database.MigrateAsync();
        var (hash, salt) = Infrastructure.Security.Passwords.Hash("Passw0rd!");
        var user = new User { Email = "face2@example.com", PasswordHash = hash, PasswordSalt = salt };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        db.UserBiometrics.Add(new UserBiometric { UserId = user.Id, Kind = AuthenticatorType.Face, Embedding = CreateVector(512), Version = "arcface_1.0", QualityScore = 0.9, LivenessMethod = LivenessMethod.PassiveV1, IsActive = true });
        db.UserBiometrics.Add(new UserBiometric { UserId = user.Id, Kind = AuthenticatorType.Face, Embedding = CreateVector(512), Version = "arcface_1.0", QualityScore = 0.85, LivenessMethod = LivenessMethod.PassiveV1, IsActive = true });
        await db.SaveChangesAsync();
        var countBefore = await db.UserBiometrics.CountAsync(b => b.UserId == user.Id);
        Assert.Equal(2, countBefore);

        db.Users.Remove(user);
        await db.SaveChangesAsync();
        var countAfter = await db.UserBiometrics.CountAsync(b => b.UserId == user.Id);
        Assert.Equal(0, countAfter);
    }
}
