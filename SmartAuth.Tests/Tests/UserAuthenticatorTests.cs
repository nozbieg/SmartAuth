using Microsoft.EntityFrameworkCore;
using SmartAuth.Domain.Entities;
using SmartAuth.Infrastructure.Security;
using SmartAuth.Tests.Helpers;

namespace SmartAuth.Tests.Tests;

public sealed class UserAuthenticatorTests(PostgresContainerFixture fx) : IClassFixture<PostgresContainerFixture>
{
    private readonly string _cs = fx.ConnectionString;

    [Fact]
    public async Task Can_create_user_with_totp_authenticator_and_generate_code()
    {
        await using var db = DbContextFactory.Create(_cs);
        await db.Database.MigrateAsync();

        var (hash, salt) = Infrastructure.Security.Passwords.Hash("Passw0rd!");
        var secret = Totp.GenerateSecret();
        var user = new User { Email = "bob@example.com", PasswordHash = hash, PasswordSalt = salt };
        var auth = new UserAuthenticator { User = user, Type = AuthenticatorType.Totp, Secret = secret };
        user.Authenticators.Add(auth);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        Assert.NotEqual(Guid.Empty, auth.Id);
        Assert.True(auth.IsActive);
        Assert.Single(user.Authenticators);

        var now = DateTimeOffset.UtcNow;
        var code = Totp.GenerateCode(secret, now);
        Assert.Equal(6, code.Length);
        Assert.True(Totp.ValidateCode(secret, code, now));
    }

    [Fact]
    public async Task Unique_constraint_on_userid_type_prevents_duplicates()
    {
        await using var db = DbContextFactory.Create(_cs);
        await db.Database.MigrateAsync();
        var (hash, salt) = Infrastructure.Security.Passwords.Hash("Passw0rd!");
        var user = new User { Email = "eve@example.com", PasswordHash = hash, PasswordSalt = salt };
        var first = new UserAuthenticator { User = user, Secret = Totp.GenerateSecret(), Type = AuthenticatorType.Totp };
        user.Authenticators.Add(first);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        db.UserAuthenticators.Add(new UserAuthenticator { UserId = user.Id, Secret = Totp.GenerateSecret(), Type = AuthenticatorType.Totp });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Deleting_user_cascades_deleting_authenticators()
    {
        await using var db = DbContextFactory.Create(_cs);
        await db.Database.MigrateAsync();
        var (hash, salt) = Infrastructure.Security.Passwords.Hash("Passw0rd!");
        var user = new User { Email = "carol@example.com", PasswordHash = hash, PasswordSalt = salt };
        var auth = new UserAuthenticator { User = user, Secret = Totp.GenerateSecret() };
        user.Authenticators.Add(auth);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        // Usuń użytkownika
        db.Users.Remove(user);
        await db.SaveChangesAsync();

        var count = await db.UserAuthenticators.CountAsync(a => a.UserId == user.Id);
        Assert.Equal(0, count);
    }

    [Fact]
    public void Totp_validation_accepts_current_and_previous_step_and_rejects_outside_window()
    {
        var secret = Totp.GenerateSecret();
        var now = DateTimeOffset.UtcNow;
        var current = Totp.GenerateCode(secret, now);
        Assert.True(Totp.ValidateCode(secret, current, now));
        
        var prevStepTime = now.AddSeconds(-30);
        var prevCode = Totp.GenerateCode(secret, prevStepTime);
        Assert.True(Totp.ValidateCode(secret, prevCode, now));

        var twoStepsAgo = now.AddSeconds(-60);
        var twoStepsCode = Totp.GenerateCode(secret, twoStepsAgo);
        Assert.False(Totp.ValidateCode(secret, twoStepsCode, now));
    }
}
