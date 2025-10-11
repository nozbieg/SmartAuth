using Microsoft.EntityFrameworkCore;
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
            opts.UseNpgsql(cs, o => o.UseVector());
        });
        return services;
    }
}