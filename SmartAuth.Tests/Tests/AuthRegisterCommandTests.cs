using Microsoft.Extensions.DependencyInjection;
using SmartAuth.Api.Features.Auth;
using SmartAuth.Api.Utilities;
using SmartAuth.Domain.Entities;
using SmartAuth.Infrastructure;
using SmartAuth.Infrastructure.Commons;

namespace SmartAuth.Tests.Tests;

public sealed class AuthRegisterCommandTests(PostgresContainerFixture fx) : IClassFixture<PostgresContainerFixture>
{
    private readonly string _cs = fx.ConnectionString;

    private async Task<AuthDbContext> MigrateAndGetDb(IServiceProvider sp)
    {
        var db = sp.GetRequiredService<AuthDbContext>();
        await db.Database.MigrateAsync();
        return db;
    }

    [Fact]
    public async Task Validation_error_when_email_missing()
    {
        var (mediator, sp) = fx.DefaultAuth;
        var cmd = new AuthRegisterCommand("", "Passw0rd!", null);
        var res = await mediator.Send<CommandResult<RegisterCompleted>>(cmd);
        Assert.False(res.IsSuccess);
        Assert.Equal("validation.error", res.Error!.Code);
        Assert.Contains("Email", res.Error!.Metadata!.Keys);
    }

    [Fact]
    public async Task Validation_error_when_password_missing()
    {
        var (mediator, sp) = fx.DefaultAuth;
        var cmd = new AuthRegisterCommand("user@example.com", "", null);
        var res = await mediator.Send<CommandResult<RegisterCompleted>>(cmd);
        Assert.False(res.IsSuccess);
        Assert.Equal("validation.error", res.Error!.Code);
        Assert.Contains("Password", res.Error!.Metadata!.Keys);
    }

    [Fact]
    public async Task Conflict_when_email_already_exists_case_insensitive()
    {
        var (mediator, sp) = fx.DefaultAuth;
        var db = await MigrateAndGetDb(sp);
        var (hash, salt) = AuthCrypto.HashPassword("Passw0rd!");
        db.Users.Add(new User
            { Email = "reguser@example.com", PasswordHash = hash, PasswordSalt = salt, Status = UserStatus.Active });
        await db.SaveChangesAsync();

        var cmd = new AuthRegisterCommand("RegUser@Example.com", "Passw0rd!", null);
        var res = await mediator.Send<CommandResult<RegisterCompleted>>(cmd);
        Assert.False(res.IsSuccess);
        Assert.Equal("common.conflict", res.Error!.Code);
    }

    [Fact]
    public async Task Success_creates_user_and_hashes_password()
    {
        var (mediator, sp) = fx.DefaultAuth;
        var db = await MigrateAndGetDb(sp);
        var email = $"new_{Guid.NewGuid():N}@example.com";
        var cmd = new AuthRegisterCommand(email, "Passw0rd!", "Display");
        var res = await mediator.Send<CommandResult<RegisterCompleted>>(cmd);

        Assert.True(res.IsSuccess);
        Assert.NotNull(res.Value);

        var created = await db.Users.FirstAsync(u => u.Email == email);
        Assert.NotNull(created.PasswordHash);
        Assert.NotNull(created.PasswordSalt);
        Assert.True(AuthCrypto.VerifyPassword("Passw0rd!", created.PasswordHash, created.PasswordSalt));
    }
}