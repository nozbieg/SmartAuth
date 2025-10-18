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
        Assert.NotNull(userEt!.GetTableName());

        var emailProp = userEt.FindProperty(nameof(User.Email));
        Assert.NotNull(emailProp);

        var emailUniqueIdx = userEt.GetIndexes()
            .FirstOrDefault(i => i.IsUnique && i.Properties.SequenceEqual(new[] { emailProp }));
        Assert.NotNull(emailUniqueIdx);
    }
    
}