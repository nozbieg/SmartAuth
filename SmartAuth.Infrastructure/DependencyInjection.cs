using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SmartAuth.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSmartAuthDbContext(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AuthDbContext>(opts =>
        {
            var cs = configuration.GetConnectionString("authdb")
                     ?? throw new InvalidOperationException("Missing AuthDb connection string");
            opts.UseNpgsql(cs, npg =>
            {
                npg.UseVector();
                npg.MigrationsAssembly("SmartAuth.Infrastructure");
            });
            opts.UseSnakeCaseNamingConvention();
        });
        return services;
    }
}