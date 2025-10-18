using Microsoft.EntityFrameworkCore.Metadata;
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

        await db.Database.MigrateAsync();

        var pending = await db.Database.GetPendingMigrationsAsync();
        Assert.Empty(pending);
    }

    private async Task<IModel> GetModelAsync()
    {
        await using var db = DbContextFactory.Create(_cs);
        await db.Database.MigrateAsync();
        var model = db.Model;
        return model;
    }

    [Fact]
    public async Task Model_User_entity_configured()
    {
        var model = await GetModelAsync();
        var userEt = model.FindEntityType(typeof(User));
        Assert.NotNull(userEt);
        Assert.NotNull(userEt.GetTableName());

        var emailProp = userEt.FindProperty(nameof(User.Email));
        Assert.NotNull(emailProp);

        var emailUniqueIdx = userEt.GetIndexes()
            .FirstOrDefault(i => i.IsUnique && i.Properties.SequenceEqual(new[] { emailProp }));
        Assert.NotNull(emailUniqueIdx);
    }

    [Fact]
    public async Task Model_UserAuthenticator_entity_configured()
    {
        var model = await GetModelAsync();
        var uaEt = model.FindEntityType(typeof(UserAuthenticator));
        Assert.NotNull(uaEt);
        Assert.Equal("user_authenticators", uaEt.GetTableName());

        var userIdProp = uaEt.FindProperty(nameof(UserAuthenticator.UserId));
        var typeProp = uaEt.FindProperty(nameof(UserAuthenticator.Type));
        Assert.NotNull(userIdProp);
        Assert.NotNull(typeProp);

        var uniqIdx = uaEt.GetIndexes().FirstOrDefault(i => i.IsUnique && i.Properties.SequenceEqual(new[] { userIdProp, typeProp }));
        Assert.NotNull(uniqIdx);
    }
}