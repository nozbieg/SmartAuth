using Microsoft.Extensions.Diagnostics.HealthChecks;
using SmartAuth.Api.Endpoints;
using SmartAuth.Api.Endpoints.Filters;
using SmartAuth.Api.Extensions;
using SmartAuth.Api.HealthChecks;
using SmartAuth.Api.Startup;
using SmartAuth.ServiceDefaults;
using SmartAuth.Api.Utilities;
using SmartAuth.Api.Services;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
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
builder.Services.Configure<TotpOptions>(builder.Configuration.GetSection("Totp"));
builder.Services.AddSingleton<IMicrosoftAuthenticatorClient, MicrosoftAuthenticatorClient>();

var app = builder.Build();

app.MapDefaultEndpoints();


app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthEndpoints();
app.UseFeatureFlagEndpoints();

app.Run();