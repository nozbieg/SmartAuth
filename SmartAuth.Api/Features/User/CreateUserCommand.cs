namespace SmartAuth.Api.Features.User;

public sealed class CreateUserCommand : IRequest<CommandResult<UserCreatedDto>>
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, MinLength(8)] public string Password { get; set; } = string.Empty;
}

public sealed record UserCreatedDto(Guid Id, string Email);

public class CreateUserCommandHandler(AuthDbContext db)
    : IRequestHandler<CreateUserCommand, CommandResult<UserCreatedDto>>
{
    public async Task<CommandResult<UserCreatedDto>> Handle(CreateUserCommand req,
        CancellationToken ct)
    {
        var exists = await db.Users.AnyAsync(u => u.Email == req.Email, cancellationToken: ct);
        if (exists) return CommandResult<UserCreatedDto>.Fail(Errors.Conflict(nameof(User)));

        var (hash, salt) = Passwords.Hash(req.Password);
        var user = new Domain.Entities.User { Email = req.Email, PasswordHash = hash, PasswordSalt = salt };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        return CommandResult<UserCreatedDto>.Ok(new UserCreatedDto(user.Id, user.Email));
    }
}