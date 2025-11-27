using SmartAuth.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

ModelFetcher.TryFetchModelsIfNeeded(builder.Configuration);

var pgPassword = builder.AddParameter(
    name: "postgres-password",
    value: Consts.DatabasePassword
);

var postgres = builder.AddPostgres(
        name: "postgres",
        password: pgPassword,
        port: Consts.DatabasePort.ToInt()
    )
    .WithImage("pgvector/pgvector:pg17")
    .WithEnvironment("PGDATA", "/var/lib/postgresql/data/pgdata")
    .WithDataVolume("auth-db-data")
    .WithBindMount("./db-init", "/docker-entrypoint-initdb.d");

var authDb = postgres.AddDatabase("authdb");

var api = builder.AddProject<Projects.SmartAuth_Api>("apiservice")
    .WithExternalHttpEndpoints()
    .WithReference(authDb)
    .WaitFor(authDb);


_ = builder.AddProject<Projects.SmartAuth_Web>("web")
    .WithReference(api)
    .WaitFor(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();
