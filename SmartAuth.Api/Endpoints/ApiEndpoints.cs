using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SmartAuth.Api.Contracts;
using SmartAuth.Api.Utilities;
using SmartAuth.Domain.Entities;
using SmartAuth.Infrastructure;
using SmartAuth.Infrastructure.Security;

namespace SmartAuth.Api.Endpoints;

public static class ApiEndpoints
{
    public static WebApplication UseApiEndpoints(this WebApplication app)
    {
        app.MapPost("/api/users", async (AuthDbContext db, CreateUserDto dto) =>
        {
            var exists = await db.Users.AnyAsync(u => u.Email == dto.Email);
            if (exists) return Results.Conflict("Email already registered");

            var (hash, salt) = Passwords.Hash(dto.Password);
            var user = new User { Email = dto.Email, PasswordHash = hash, PasswordSalt = salt };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            return Results.Created($"/api/users/{user.Id}", new { user.Id, user.Email });
        });

        app.MapPost("/api/authenticators", async (AuthDbContext db, AddAuthenticatorDto dto, ClaimsPrincipal _) =>
        {
            var userId = /* z JWT / sesji */ dto.UserId; // demo: użyj z body, docelowo z kontekstu
            var auth = new UserAuthenticator
            {
                UserId = userId, Type = dto.Type, DisplayName = dto.DisplayName
            };
            db.UserAuthenticators.Add(auth);
            await db.SaveChangesAsync();
            return Results.Ok(new { auth.Id, auth.Type, auth.DisplayName });
        });


        app.MapPost("/api/enroll/face/complete", async (AuthDbContext db, FaceEnrollCompleteDto dto) =>
        {
            var auth = await db.UserAuthenticators
                .FirstOrDefaultAsync(a => a.Id == dto.AuthenticatorId && a.Type == AuthenticatorType.Face);
            if (auth is null) return Results.NotFound();

            var tmpl = new FaceTemplate
            {
                AuthenticatorId = auth.Id,
                Embedding = dto.Embedding.ToVector(),
                QualityScore = dto.QualityScore,
                ModelVersion = dto.ModelVersion ?? "face-v1"
            };

            db.FaceTemplates.Add(tmpl);
            auth.IsEnrolled = true;
            auth.UpdatedAt = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(new { auth.Id, templateId = tmpl.Id });
        });

        app.MapPost("/api/auth/face", async (AuthDbContext db, FaceVerifyDto dto) =>
        {
            var user = await db.Users.Include(u => u.Authenticators)
                .ThenInclude(a => a.FaceTemplate).ThenInclude(faceTemplate => faceTemplate!.Embedding)
                .FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user is null) return Results.NotFound();

            var faceAuth = user.Authenticators.FirstOrDefault(a => a.Type == AuthenticatorType.Face && a.IsEnrolled);
            if (faceAuth?.FaceTemplate is null) return Results.BadRequest("Face not enrolled");

            var score = Utility.CosineSimilarity(faceAuth.FaceTemplate.Embedding, dto.Embedding.ToVector());
            var success = score >= dto.RequiredScore;

            db.AuthAttempts.Add(new AuthAttempt
            {
                UserId = user.Id,
                Type = AttemptType.Face,
                Success = success,
                Score = score,
                Ip = dto.Ip ?? "",
                UserAgent = dto.UserAgent ?? ""
            });
            await db.SaveChangesAsync();

            return Results.Ok(new { success, score });
        });

        return app;
    }
}