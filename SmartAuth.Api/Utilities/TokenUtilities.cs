using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SmartAuth.Api.Utilities;

public static class TokenUtilities
{
    public static string IssueAccessToken(IConfiguration cfg, string subject, string displayName)
    {
        var jwtSec = cfg.GetSection("Jwt");
        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSec["Key"]!)),
            SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, subject),
            new Claim("email", subject),
            new Claim("name", displayName),
            new Claim("role", "user")
        };
        var token = new JwtSecurityToken(
            issuer: jwtSec["Issuer"],
            audience: jwtSec["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(int.Parse(jwtSec["AccessTokenMinutes"]!)),
            signingCredentials: creds
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string IssueTempToken(IConfiguration cfg, string subject)
    {
        var jwtSec = cfg.GetSection("Jwt");
        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSec["Key"]!)),
            SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, subject),
            new Claim("typ", "temp")
        };
        var token = new JwtSecurityToken(
            issuer: jwtSec["Issuer"],
            audience: jwtSec["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(int.Parse(jwtSec["TempTokenMinutes"]!)),
            signingCredentials: creds
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }


    public static string? GetSubjectFromToken(HttpContext ctx)
    {
        return ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
    }
}