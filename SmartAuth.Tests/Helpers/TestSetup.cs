using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartAuth.Api.Extensions;
using SmartAuth.Api.Utilities;
using SmartAuth.Domain.Entities;
using SmartAuth.Infrastructure;
using SmartAuth.Infrastructure.Commons;
using SmartAuth.Infrastructure.Security;

namespace SmartAuth.Tests.Helpers;

public static class TestSetup
{
    public static Dictionary<string,string?> BaseJwtSettings => new()
    {
        ["Jwt:Key"] = "12345678901234567890123456789012",
        ["Jwt:Issuer"] = "test-issuer",
        ["Jwt:Audience"] = "test-audience",
        ["Jwt:AccessTokenMinutes"] = "60",
        ["Jwt:TempTokenMinutes"] = "5"
    };

    public static IConfiguration BuildConfig(bool twoFaCodeEnabled = false, IDictionary<string,string?>? overrides = null)
    {
        var dict = new Dictionary<string,string?>(BaseJwtSettings)
        {
            ["FeatureFlags:twofa_code"] = twoFaCodeEnabled ? "true" : "false"
        };
        if (overrides is not null)
            foreach (var kv in overrides) dict[kv.Key] = kv.Value;
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    public static async Task<AuthDbContext> EnsureMigratedAsync(IServiceProvider sp)
    {
        var db = sp.GetRequiredService<AuthDbContext>();
        await db.Database.MigrateAsync();
        return db;
    }

    public static User CreateUserEntity(string email, string password = "Passw0rd!", bool totpActive = false)
    {
        var (hash, salt) = AuthCrypto.HashPassword(password);
        var user = new User { Email = email, PasswordHash = hash, PasswordSalt = salt, Status = UserStatus.Active };
        if (totpActive)
        {
            user.Authenticators.Add(new UserAuthenticator
            {
                Type = AuthenticatorType.Totp,
                Secret = Totp.GenerateSecret(),
                IsActive = true
            });
        }
        return user;
    }

    public static async Task<User> AddUserAsync(AuthDbContext db, string email, string password = "Passw0rd!", bool totpActive = false)
    {
        var user = CreateUserEntity(email, password, totpActive);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    public static (ServiceProvider provider, IMediator mediator) BuildMediator(string cs, bool twoFaCodeEnabled = false, IDictionary<string,string?>? overrides = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(BuildConfig(twoFaCodeEnabled, overrides));
        services.AddDbContext<AuthDbContext>(o =>
        {
            o.UseNpgsql(cs, npg => npg.UseVector());
            o.UseSnakeCaseNamingConvention();
            o.EnableSensitiveDataLogging();
        });
        services.AddScoped<IMediator, Mediator>();
        services.AddHandlers();
        services.AddValidators();
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();
        return ((ServiceProvider)sp, mediator);
    }

    public static (IHttpContextAccessor accessor, ServiceProvider provider, IConfiguration cfg) BuildHttpContextWithUser(string cs, string? emailClaim, bool twoFaCodeEnabled = true, IDictionary<string,string?>? overrides = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(TimeProvider.System);
        var cfg = BuildConfig(twoFaCodeEnabled, overrides);
        services.AddSingleton<IConfiguration>(cfg);
        services.AddDbContext<AuthDbContext>(o =>
        {
            o.UseNpgsql(cs, npg => npg.UseVector());
            o.UseSnakeCaseNamingConvention();
            o.EnableSensitiveDataLogging();
        });
        var sp = services.BuildServiceProvider();
        var ctx = new DefaultHttpContext { RequestServices = sp };
        if (!string.IsNullOrWhiteSpace(emailClaim))
        {
            var claims = new[] { new Claim(JwtRegisteredClaimNames.Sub, emailClaim) };
            ctx.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        }
        var accessor = new HttpContextAccessor { HttpContext = ctx };
        return (accessor, (ServiceProvider)sp, cfg);
    }
}

