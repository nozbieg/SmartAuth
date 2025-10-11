using Microsoft.Extensions.Diagnostics.HealthChecks;
using SmartAuth.Api.Extensions;
using SmartAuth.Api.HealthChecks;
using SmartAuth.Api.Startup;
using SmartAuth.Infrastructure;
using SmartAuth.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSmartAuthDbContext(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddCheck<DbConnectivityHealthCheck>("postgres", tags: ["ready"])
    .AddDbContextCheck<AuthDbContext>(name: "postgres_context", failureStatus: HealthStatus.Unhealthy, tags: ["ready"])
    .AddCheck<PendingMigrationsHealthCheck>(name: "migrations", tags: ["ready"]);

builder.Services.AddHostedService<MigrationRunnerHostedService>();

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseSwagger();
app.UseSwaggerUI();

app.UseApiEndpoints();

app.MapGet("/api/hello", () => new { message = "Hello from .NET 9 API 👋" });

app.Run();