using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartAuth.Infrastructure.Commons;

namespace SmartAuth.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSmartAuthDbContext(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IMediator, Mediator>();
        
        AddDbContext(services, configuration);
        return services;
    }

    private static IServiceCollection AddDbContext(IServiceCollection services, IConfiguration configuration)
    {
        return services.AddDbContext<AuthDbContext>(opts =>
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
    }
}