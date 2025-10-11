using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SmartAuth.Infrastructure;

public sealed class DesignTimeFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args)
    {
        var cs =
            Environment.GetEnvironmentVariable("AUTHDB_CS")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__authdb")
            ?? "Host=localhost;Port=5432;Database=authdb;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseNpgsql(cs, npg =>
            {
                npg.UseVector();                   
                npg.MigrationsAssembly("SmartAuth.Infrastructure"); 
            })
            .UseSnakeCaseNamingConvention()      
            .EnableSensitiveDataLogging()         
            .Options;

        return new AuthDbContext(options);
    }
}