using SmartAuth.Api.Features.Auth;
using SmartAuth.Api.Utilities;
using SmartAuth.Domain.Entities;
using SmartAuth.Infrastructure.Commons;
using SmartAuth.Infrastructure.Security;
using SmartAuth.Tests.Helpers;

namespace SmartAuth.Tests.Tests;

public sealed class AuthLoginCommandTests(PostgresContainerFixture fx) : IClassFixture<PostgresContainerFixture>
{
    private readonly string _cs = fx.ConnectionString;

    [Fact]
    public async Task Validation_error_when_email_missing()
    {
        var (mediator, sp) = fx.DefaultAuth;
        var cmd = new AuthLoginCommand("", "pass");
        var res = await mediator.Send<CommandResult<AuthLoginResult>>(cmd);
        Assert.False(res.IsSuccess);
        Assert.Equal("validation.error", res.Error!.Code);
    }

    [Fact]
    public async Task Not_found_when_user_missing()
    {
        var (mediator, sp) = fx.DefaultAuth;
        await TestSetup.EnsureMigratedAsync(sp);
        var cmd = new AuthLoginCommand("nouser@example.com", "Passw0rd!");
        var res = await mediator.Send<CommandResult<AuthLoginResult>>(cmd);
        Assert.False(res.IsSuccess);
        Assert.Equal("common.not_found", res.Error!.Code);
    }

    [Fact]
    public async Task Forbidden_when_user_not_active()
    {
        var (mediator, sp) = fx.DefaultAuth;
        var db = await TestSetup.EnsureMigratedAsync(sp);
        var (hash, salt) = AuthCrypto.HashPassword("Passw0rd!");
        db.Users.Add(new User { Email = "locked@example.com", PasswordHash = hash, PasswordSalt = salt, Status = UserStatus.Locked });
        await db.SaveChangesAsync();

        var cmd = new AuthLoginCommand("locked@example.com", "Passw0rd!");
        var res = await mediator.Send<CommandResult<AuthLoginResult>>(cmd);
        Assert.False(res.IsSuccess);
        Assert.Equal("auth.forbidden", res.Error!.Code);
    }

    [Fact]
    public async Task Invalid_credentials_when_password_incorrect()
    {
        var (mediator, sp) = fx.DefaultAuth;
        var db = await TestSetup.EnsureMigratedAsync(sp);
        var (hash, salt) = AuthCrypto.HashPassword("Correct1!");
        db.Users.Add(new User { Email = "alice@example.com", PasswordHash = hash, PasswordSalt = salt, Status = UserStatus.Active });
        await db.SaveChangesAsync();

        var cmd = new AuthLoginCommand("alice@example.com", "WrongPwd");
        var res = await mediator.Send<CommandResult<AuthLoginResult>>(cmd);
        Assert.False(res.IsSuccess);
        Assert.Equal("auth.invalid_credentials", res.Error!.Code);
    }

    [Fact]
    public async Task Success_without_2fa_returns_access_token()
    {
        var (mediator, sp) = fx.DefaultAuth;
        var db = await TestSetup.EnsureMigratedAsync(sp);
        var (hash, salt) = AuthCrypto.HashPassword("Correct1!");
        var email = $"user_{Guid.NewGuid():N}@example.com";
        db.Users.Add(new User { Email = email, PasswordHash = hash, PasswordSalt = salt, Status = UserStatus.Active });
        await db.SaveChangesAsync();

        var cmd = new AuthLoginCommand(email, "Correct1!");
        var res = await mediator.Send<CommandResult<AuthLoginResult>>(cmd);

        Assert.True(res.IsSuccess);
        Assert.NotNull(res.Value);
        Assert.False(res.Value!.Requires2Fa);
        Assert.False(string.IsNullOrWhiteSpace(res.Value.Token));
        Assert.True(res.Value.Methods is null or { Count: 0 });

        var userReloaded = await db.Users.FirstAsync(u => u.Email == email);
        Assert.NotNull(userReloaded.LastLoginAt);
    }

    [Fact]
    public async Task Success_with_2fa_returns_temp_token_and_methods()
    {
        var (mediator, sp) = fx.CreateAuthMediator(twoFaEnabled: true);
        var db = await TestSetup.EnsureMigratedAsync(sp);
        var (hash, salt) = AuthCrypto.HashPassword("Correct1!");
        var email = $"user_{Guid.NewGuid():N}@example.com";
        db.Users.Add(new User { Email = email, PasswordHash = hash, PasswordSalt = salt, Status = UserStatus.Active });
        await db.SaveChangesAsync();

        var cmd = new AuthLoginCommand(email, "Correct1!");
        var res = await mediator.Send<CommandResult<AuthLoginResult>>(cmd);

        Assert.True(res.IsSuccess);
        Assert.NotNull(res.Value);
        Assert.True(res.Value!.Requires2Fa);
        Assert.Contains("code", res.Value.Methods!);
        Assert.False(string.IsNullOrWhiteSpace(res.Value.Token));
    }

    [Fact]
    public async Task Success_with_active_totp_returns_temp_token_and_totp_method_only_when_code_flag_disabled()
    {
        var (mediator, sp) = fx.DefaultAuth;
        var db = await TestSetup.EnsureMigratedAsync(sp);
        var (hash, salt) = AuthCrypto.HashPassword("Correct1!");
        var email = $"totpactive_{Guid.NewGuid():N}@example.com";
        var user = new User { Email = email, PasswordHash = hash, PasswordSalt = salt, Status = UserStatus.Active };
        user.Authenticators.Add(new UserAuthenticator { Type = AuthenticatorType.Totp, Secret = Totp.GenerateSecret(), IsActive = true });
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var cmd = new AuthLoginCommand(email, "Correct1!");
        var res = await mediator.Send<CommandResult<AuthLoginResult>>(cmd);

        Assert.True(res.IsSuccess);
        Assert.NotNull(res.Value);
        Assert.True(res.Value!.Requires2Fa);
        Assert.Contains("totp", res.Value.Methods!);
        Assert.DoesNotContain("code", res.Value.Methods!);
        Assert.False(string.IsNullOrWhiteSpace(res.Value.Token));
    }
}
