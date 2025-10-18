using Microsoft.Extensions.Diagnostics.HealthChecks;
using SmartAuth.Api.Endpoints;
using SmartAuth.Api.Extensions;
using SmartAuth.Api.Features;
using SmartAuth.Api.HealthChecks;
using SmartAuth.Api.Middlewares;
using SmartAuth.Api.Startup;
using SmartAuth.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSJwtSmartAuth(builder.Configuration);

builder.AddServiceDefaults();

builder.Services.AddSingleton<MediatorEndpointFilter>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSmartAuthDbContext(builder.Configuration);

builder.Services.AddHandlers();
builder.Services.AddValidators();

builder.Services.AddHealthChecks()
    .AddCheck<DbConnectivityHealthCheck>("postgres", tags: ["ready"])
    .AddDbContextCheck<AuthDbContext>(name: "postgres_context", failureStatus: HealthStatus.Unhealthy, tags: ["ready"])
    .AddCheck<PendingMigrationsHealthCheck>(name: "migrations", tags: ["ready"]);

builder.Services.AddHostedService<MigrationRunnerHostedService>();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseMiddleware<TraceHeadersMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();
app.UseApiEndpoints();
app.Use2FaEndpoints();
app.UseFeatureFlagEndpoints();

app.Run();