using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SmartAuth.Infrastructure;

public sealed class DesignTimeFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("auuthdb")
                 ?? "Host=localhost;Port=5432;Database=authdb;Username=postgres;Password=postgres";

        var opt = new DbContextOptionsBuilder<AuthDbContext>()
            .UseNpgsql(cs, o => o.UseVector())
            .Options;

        return new(opt);
    }
}