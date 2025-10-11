using SmartAuth.Tests.Helpers;

namespace SmartAuth.Tests.Tests;

public class SchemaTests : IClassFixture<PostgresContainerFixture>
{
    private readonly string _cs;
    public SchemaTests(PostgresContainerFixture fx) => _cs = fx.ConnectionString;

    [Fact]
    public async Task Migrations_create_schema_and_pgvector_extension()
    {
        await using var db = DbContextFactory.Create(_cs);

        // apply migrations
        await db.Database.MigrateAsync();

        // vector extension present?
        var vectorExists = await db.Database
            .SqlQueryRaw<int>("SELECT 1 FROM pg_extension WHERE extname = 'vector'")
            .AnyAsync();
        Assert.True(vectorExists, "pgvector extension must be available");

        // Users table exists?
        var usersExists = await db.Database
            .SqlQueryRaw<int>("SELECT 1 FROM pg_tables WHERE tablename = 'Users'")
            .AnyAsync();
        Assert.True(usersExists, "Users table should exist");

        // Embedding column type = vector(512) in FaceTemplates
        var colType = await db.Database
            .SqlQueryRaw<string>(
                """
                SELECT format_type(a.atttypid, a.atttypmod)
                FROM pg_attribute a
                JOIN pg_class c ON a.attrelid = c.oid
                WHERE c.relname = 'FaceTemplates' AND a.attname = 'Embedding' AND a.attnum > 0
                LIMIT 1
                """)
            .FirstOrDefaultAsync();

        Assert.Equal("vector(512)", colType);
    }
}