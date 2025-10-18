using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartAuth.Api.Extensions;
using SmartAuth.Infrastructure;
using SmartAuth.Infrastructure.Commons;

namespace SmartAuth.Tests.Helpers;

public static class TestDiFactory
{
    public static (IMediator mediator, IServiceProvider provider) CreateAuthMediator(string connectionString, bool twoFaEnabled, IDictionary<string,string?>? extraConfig = null)
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSingleton(TimeProvider.System);

        var baseSettings = new Dictionary<string, string?>
        {
            ["FeatureFlags:twofa_code"] = twoFaEnabled ? "true" : "false",
            ["Jwt:Key"] = "12345678901234567890123456789012",
            ["Jwt:Issuer"] = "test-issuer",
            ["Jwt:Audience"] = "test-audience",
            ["Jwt:AccessTokenMinutes"] = "60",
            ["Jwt:TempTokenMinutes"] = "5"
        };
        if (extraConfig is not null)
        {
            foreach (var kv in extraConfig)
                baseSettings[kv.Key] = kv.Value; 
        }
        IConfiguration cfg = new ConfigurationBuilder().AddInMemoryCollection(baseSettings).Build();
        services.AddSingleton(cfg);

        services.AddDbContext<AuthDbContext>(o =>
        {
            o.UseNpgsql(connectionString, npg => npg.UseVector());
            o.UseSnakeCaseNamingConvention();
            o.EnableSensitiveDataLogging();
        });

        services.AddScoped<IMediator, Mediator>();
        services.AddHandlers();
        services.AddValidators();

        var provider = services.BuildServiceProvider();
        return (provider.GetRequiredService<IMediator>(), provider);
    }
}
