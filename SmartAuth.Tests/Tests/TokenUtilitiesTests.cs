using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SmartAuth.Api.Utilities;
using SmartAuth.Tests.Helpers;

namespace SmartAuth.Tests.Tests;

public class TokenUtilitiesTests
{
    [Fact]
    public void Issue_access_token_contains_expected_claims()
    {
        var cfg = TestSetup.BuildConfig();
        var jwtStr = TokenUtilities.IssueAccessToken(cfg, "user@example.com", "Display Name");
        var token = new JwtSecurityTokenHandler().ReadJwtToken(jwtStr);
        Assert.Contains(token.Claims, c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == "user@example.com");
        Assert.Contains(token.Claims, c => c.Type == "email" && c.Value == "user@example.com");
        Assert.Contains(token.Claims, c => c.Type == "name" && c.Value == "Display Name");
        Assert.Contains(token.Claims, c => c.Type == "role" && c.Value == "user");
        Assert.DoesNotContain(token.Claims, c => c.Type == "typ" && c.Value == "temp");
    }

    [Fact]
    public void Issue_temp_token_contains_temp_claim()
    {
        var cfg = TestSetup.BuildConfig();
        var jwtStr = TokenUtilities.IssueTempToken(cfg, "temp@example.com");
        var token = new JwtSecurityTokenHandler().ReadJwtToken(jwtStr);
        Assert.Contains(token.Claims, c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == "temp@example.com");
        Assert.Contains(token.Claims, c => c.Type == "typ" && c.Value == "temp");
    }

    [Fact]
    public void Get_subject_from_context_prefers_nameidentifier_then_sub()
    {
        var cfg = TestSetup.BuildConfig();
        var claims1 = new[] { new Claim(ClaimTypes.NameIdentifier, "n1"), new Claim(JwtRegisteredClaimNames.Sub, "sub1") };
        var principal1 = new ClaimsPrincipal(new ClaimsIdentity(claims1, "t"));
        var ctx1 = new DefaultHttpContext { User = principal1 };
        Assert.Equal("n1", TokenUtilities.GetSubjectFromToken(ctx1));

        var claims2 = new[] { new Claim(JwtRegisteredClaimNames.Sub, "sub2") };
        var principal2 = new ClaimsPrincipal(new ClaimsIdentity(claims2, "t"));
        var ctx2 = new DefaultHttpContext { User = principal2 };
        Assert.Equal("sub2", TokenUtilities.GetSubjectFromToken(ctx2));
    }
}
