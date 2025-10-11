namespace SmartAuth.Tests;

public sealed class PostgresContainerFixture : IAsyncLifetime
{
    public PostgreSqlContainer Container { get; private set; } = default!;
    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        Container = new PostgreSqlBuilder()
            .WithImage("pgvector/pgvector:pg17")
            .WithDatabase("authdb")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithEnvironment("PGDATA", "/var/lib/postgresql/data/pgdata")
            .Build();

        await Container.StartAsync();
        ConnectionString = Container.GetConnectionString();
    }

    public async Task DisposeAsync()
    {
        await Container.DisposeAsync();
    }
}