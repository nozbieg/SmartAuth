using SmartAuth.Infrastructure.Commons;
using SmartAuth.Tests.Helpers;

namespace SmartAuth.Tests;

public sealed class PostgresContainerFixture : IAsyncLifetime
{
    public PostgreSqlContainer Container { get; private set; } = default!;
    public string ConnectionString { get; private set; } = string.Empty;

    public (IMediator mediator, IServiceProvider provider) DefaultAuth { get; private set; }

    public (IMediator mediator, IServiceProvider provider) CreateAuthMediator(bool twoFaEnabled, IDictionary<string,string?>? extraConfig = null)
        => TestDiFactory.CreateAuthMediator(ConnectionString, twoFaEnabled, extraConfig);


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

        DefaultAuth = TestDiFactory.CreateAuthMediator(ConnectionString, twoFaEnabled: false);
    }

    public async Task DisposeAsync()
    {
        await Container.DisposeAsync();
    }
}