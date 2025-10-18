using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SmartAuth.Api.Contracts;
using SmartAuth.Api.Utilities;
using SmartAuth.Domain.Entities;
using SmartAuth.Infrastructure;
using LoginRequest = Microsoft.AspNetCore.Identity.Data.LoginRequest;

namespace SmartAuth.Api.Endpoints;

public static class AuthEndpoints
{
    public static WebApplication Use2FaEndpoints(this WebApplication app)
    {
        app.MapPost("/api/auth/register", async (RegisterRequest req, AuthDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                return Results.BadRequest("Email i hasło są wymagane.");

            var emailNorm = req.Email.Trim().ToLowerInvariant();

            var exists = await db.Users.AnyAsync(u => u.Email.ToLower() == emailNorm);
            if (exists)
                return Results.Conflict("Użytkownik z takim adresem email już istnieje.");

            var (hash, salt) = AuthCrypto.HashPassword(req.Password);

            var user = new User
            {
                Email = req.Email.Trim(),
                PasswordHash = hash,
                PasswordSalt = salt,
                Status = UserStatus.Active,
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();

            return Results.Created($"/api/users/{user.Id}", new { message = "Konto utworzone. Zaloguj się." });
        });

        app.MapPost("/api/auth/login", async (LoginRequest req, AuthDbContext db, IConfiguration cfg) =>
        {
            var emailNorm = req.Email.Trim().ToLowerInvariant();

            var user = await db.Users
                .Include(u => u.Authenticators)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == emailNorm);

            if (user is null) return Results.Unauthorized();
            if (user.Status != UserStatus.Active) return Results.Forbid();

            var ok = AuthCrypto.VerifyPassword(req.Password, user.PasswordHash, user.PasswordSalt);
            if (!ok) return Results.Unauthorized();

            var flags = cfg.GetSection("FeatureFlags").Get<FeatureFlags>()!;
            var methods = new List<string>();
            if (flags.twofa_code) methods.Add("code");
            if (flags.twofa_face) methods.Add("face");
            if (flags.twofa_voice) methods.Add("voice");

            user.LastLoginAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();

            if (methods.Count == 0)
            {
                var jwt = IssueAccessToken(cfg, user.Email, user.Email);
                return Results.Json(new { requires2FA = false, jwt });
            }
            else
            {
                var temp = IssueTempToken(cfg, user.Email);
                return Results.Json(new { requires2FA = true, methods, tempToken = temp });
            }
        });

        app.MapPost("/api/auth/2fa/code/verify", (HttpContext ctx, CodeVerifyRequest body, IConfiguration cfg) =>
        {
            var email = GetSubjectFromToken(ctx);
            if (email is null) return Results.Unauthorized();


            // TODO replace with real TOTP or delivered code
            if (body.Code != "123456") return Results.Unauthorized();


            var jwt = IssueAccessToken(cfg, email, email);
            return Results.Json(new { jwt });
        }).RequireAuthorization();

        app.MapPost("/api/auth/2fa/face/verify", async (HttpContext ctx, IConfiguration cfg) =>
        {
            var email = GetSubjectFromToken(ctx);
            if (email is null) return Results.Unauthorized();


// Expect multipart/form-data with file key "image"
            if (!ctx.Request.HasFormContentType) return Results.BadRequest();
            var form = await ctx.Request.ReadFormAsync();
            var file = form.Files.GetFile("image");
            if (file is null || file.Length == 0) return Results.BadRequest();


// TODO: run ONNX face verification against enrolled template
            var match = true; // stub
            if (!match) return Results.Unauthorized();


            var jwt = IssueAccessToken(cfg, email, email);
            return Results.Json(new { jwt });
        }).RequireAuthorization();

        app.MapPost("/api/auth/2fa/voice/verify", async (HttpContext ctx, IConfiguration cfg) =>
        {
            var email = GetSubjectFromToken(ctx);
            if (email is null) return Results.Unauthorized();


            if (!ctx.Request.HasFormContentType) return Results.BadRequest();
            var form = await ctx.Request.ReadFormAsync();
            var file = form.Files.GetFile("audio");
            if (file is null || file.Length == 0) return Results.BadRequest();


// TODO: run ONNX voice verification against enrolled template
            var match = true; // stub
            if (!match) return Results.Unauthorized();


            var jwt = IssueAccessToken(cfg, email, email);
            return Results.Json(new { jwt });
        }).RequireAuthorization();

        return app;
    }

    static string IssueAccessToken(IConfiguration cfg, string subject, string displayName)
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

    static string IssueTempToken(IConfiguration cfg, string subject)
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


    static string? GetSubjectFromToken(HttpContext ctx)
    {
        return ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
    }
}