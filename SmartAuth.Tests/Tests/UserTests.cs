using SmartAuth.Domain.Entities;
using SmartAuth.Infrastructure.Security;
using SmartAuth.Tests.Helpers;

namespace SmartAuth.Tests.Tests;

public sealed class UserTests(PostgresContainerFixture fx) : IClassFixture<PostgresContainerFixture>
{
    private readonly string _cs = fx.ConnectionString;

    [Fact]
    public async Task Create_user_hashes_password_and_enforces_unique_email()
    {
        await using var db = DbContextFactory.Create(_cs);
        await db.Database.MigrateAsync();

        var (hash, salt) = Passwords.Hash("Passw0rd!");
        var u1 = new User { Email = "alice@example.com", PasswordHash = hash, PasswordSalt = salt };

        db.Users.Add(u1);
        await db.SaveChangesAsync();

        Assert.NotNull(u1.PasswordHash);
        Assert.NotNull(u1.PasswordSalt);
        Assert.True(Passwords.Verify("Passw0rd!", u1.PasswordHash, u1.PasswordSalt));
        Assert.False(Passwords.Verify("wrong", u1.PasswordHash, u1.PasswordSalt));

        db.Users.Add(new User { Email = "alice@example.com", PasswordHash = hash, PasswordSalt = salt });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
}