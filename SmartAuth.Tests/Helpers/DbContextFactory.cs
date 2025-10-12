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
                // npg.MigrationsAssembly("SmartAuth.Infrastructure"); // niekonieczne, ale pozostawione jako komentarz.
            })
            .UseSnakeCaseNamingConvention() // zapewnia zgodność z migracjami / snapshotem
            .EnableSensitiveDataLogging()
            .Options;

        return new AuthDbContext(options);
    }
}