using SmartAuth.Api.Utilities;

namespace SmartAuth.Api.Features.Auth;

public record AuthRegisterCommand(string Email, string Password, string? DisplayName)
    : IRequest<CommandResult<RegisterCompleted>>;

public record RegisterCompleted(string Message = Messages.Success.RegistrationCompleted);

public class AuthRegisterValidator : Validator<AuthRegisterCommand>
{
    protected override Task ValidateParams(AuthRegisterCommand request)
    {
        if (string.IsNullOrWhiteSpace(request.Email)) Metadata.Add(nameof(request.Email), Messages.Validation.EmailRequired);
        if (string.IsNullOrWhiteSpace(request.Password)) Metadata.Add(nameof(request.Password), Messages.Validation.PasswordRequired);
        return Task.CompletedTask;
    }
}

public class AuthRegisterCommandHandler(AuthDbContext db)
    : IRequestHandler<AuthRegisterCommand, CommandResult<RegisterCompleted>>
{
    public async Task<CommandResult<RegisterCompleted>> Handle(AuthRegisterCommand req, CancellationToken ct)
    {
        var emailNorm = req.Email.Trim().ToLowerInvariant();

        var exists = await db.Users.AnyAsync(u => u.Email.ToLower() == emailNorm, cancellationToken: ct);
        if (exists)
            return CommandResult<RegisterCompleted>.Fail(Errors.Conflict(nameof(req.Email)));

        var (hash, salt) = AuthCrypto.HashPassword(req.Password);

        var user = new User
        {
            Email = emailNorm,
            PasswordHash = hash,
            PasswordSalt = salt,
            Status = UserStatus.Active,
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return CommandResult<RegisterCompleted>.Ok(new RegisterCompleted());
    }
}