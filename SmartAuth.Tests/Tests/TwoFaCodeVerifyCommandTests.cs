using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SmartAuth.Api.Features.Auth;
using SmartAuth.Infrastructure;
using SmartAuth.Infrastructure.Security;
using SmartAuth.Tests.Helpers;

namespace SmartAuth.Tests.Tests;

public class TwoFaCodeVerifyCommandTests(PostgresContainerFixture fx) : IClassFixture<PostgresContainerFixture>
{
    private readonly string _cs = fx.ConnectionString;

    private async Task<(IHttpContextAccessor accessor, AuthDbContext db, IConfiguration cfg)> BuildContextAsync(string? emailClaim, bool twoFaCodeEnabled)
    {
        var (accessor, sp, cfg) = TestSetup.BuildHttpContextWithUser(_cs, emailClaim, twoFaCodeEnabled);
        var db = await TestSetup.EnsureMigratedAsync(sp);
        return (accessor, db, cfg);
    }

    [Fact]
    public async Task Unauthorized_when_claim_missing()
    {
        var (accessor, db, cfg) = await BuildContextAsync(null, true);
        var handler = new TwoFaCodeVerifyCommandHandler(accessor, cfg);
        var res = await handler.Handle(new TwoFaCodeVerifyCommand("123456"), CancellationToken.None);
        Assert.False(res.IsSuccess);
        Assert.Equal("auth.unauthorized", res.Error!.Code);
    }

    [Fact]
    public async Task Not_found_when_user_missing()
    {
        var (accessor, db, cfg) = await BuildContextAsync("nouser@example.com", true);
        var handler = new TwoFaCodeVerifyCommandHandler(accessor, cfg);
        var res = await handler.Handle(new TwoFaCodeVerifyCommand("000000"), CancellationToken.None);
        Assert.False(res.IsSuccess);
        Assert.Equal("common.not_found", res.Error!.Code);
    }

    [Fact]
    public async Task Success_totp_code_valid_returns_access_token()
    {
        var email = $"totp_{Guid.NewGuid():N}@example.com";
        var (accessor, db, cfg) = await BuildContextAsync(email, true);
        var user = await TestSetup.AddUserAsync(db, email, totpActive: true);
        var totpSecret = user.Authenticators.First().Secret;
        var code = Totp.GenerateCode(totpSecret);
        var handler = new TwoFaCodeVerifyCommandHandler(accessor, cfg);
        var res = await handler.Handle(new TwoFaCodeVerifyCommand(code), CancellationToken.None);
        Assert.True(res.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(res.Value!.Jwt));
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(res.Value!.Jwt);
        Assert.Contains(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == email);
        Assert.DoesNotContain(jwt.Claims, c => c.Type == "typ" && c.Value == "temp");
    }

    [Fact]
    public async Task Success_fallback_code_flow_when_no_totp_and_flag_enabled()
    {
        var email = $"codeonly_{Guid.NewGuid():N}@example.com";
        var (accessor, db, cfg) = await BuildContextAsync(email, true);
        await TestSetup.AddUserAsync(db, email, totpActive: false);
        var handler = new TwoFaCodeVerifyCommandHandler(accessor, cfg);
        var res = await handler.Handle(new TwoFaCodeVerifyCommand("any"), CancellationToken.None);
        Assert.True(res.IsSuccess);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(res.Value!.Jwt);
        Assert.DoesNotContain(jwt.Claims, c => c.Type == "typ" && c.Value == "temp");
    }

    [Fact]
    public async Task Invalid_credentials_when_code_wrong_and_totp_exists()
    {
        var email = $"wrong_{Guid.NewGuid():N}@example.com";
        var (accessor, db, cfg) = await BuildContextAsync(email, true);
        await TestSetup.AddUserAsync(db, email, totpActive: true);
        var handler = new TwoFaCodeVerifyCommandHandler(accessor, cfg);
        var res = await handler.Handle(new TwoFaCodeVerifyCommand("000000"), CancellationToken.None);
        Assert.False(res.IsSuccess);
        Assert.Equal("auth.invalid_credentials", res.Error!.Code);
    }

    [Fact]
    public async Task Invalid_credentials_when_flag_disabled_and_no_totp()
    {
        var email = $"noflag_{Guid.NewGuid():N}@example.com";
        var (accessor, db, cfg) = await BuildContextAsync(email, false);
        await TestSetup.AddUserAsync(db, email, totpActive: false);
        var handler = new TwoFaCodeVerifyCommandHandler(accessor, cfg);
        var res = await handler.Handle(new TwoFaCodeVerifyCommand("whatever"), CancellationToken.None);
        Assert.False(res.IsSuccess);
        Assert.Equal("auth.invalid_credentials", res.Error!.Code);
    }
}
