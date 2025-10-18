using SmartAuth.Infrastructure;

namespace SmartAuth.Tests.Helpers;

public static class DbContextFactory
{
    public static AuthDbContext Create(string cs)
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseNpgsql(cs, npg =>
            {
                npg.UseVector();
            })
            .UseSnakeCaseNamingConvention() 
            .EnableSensitiveDataLogging()
            .Options;

        return new AuthDbContext(options, TimeProvider.System);
    }
}