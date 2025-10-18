using SmartAuth.Api.Utilities;

namespace SmartAuth.Api.Features.Auth;

public record TwoFaCodeVerifyCommand(string Code) : IRequest<CommandResult<TwoFaCodeVerifyResult>>;

public record TwoFaCodeVerifyResult(string Jwt);

public class TwoFaCodeVerifyValidator : Validator<TwoFaCodeVerifyCommand>
{
    protected override Task ValidateParams(TwoFaCodeVerifyCommand request)
    {
        if (string.IsNullOrEmpty(request.Code)) Metadata.Add(nameof(request.Code), "Code is required");
        return Task.CompletedTask;
    }
}

public class
    TwoFaCodeVerifyCommandHandler(IHttpContextAccessor accessor, IConfiguration cfg)
    : IRequestHandler<TwoFaCodeVerifyCommand, CommandResult<TwoFaCodeVerifyResult>>
{
    public Task<CommandResult<TwoFaCodeVerifyResult>> Handle(TwoFaCodeVerifyCommand req,
        CancellationToken cancellationToken)
    {
        var ctx = accessor.HttpContext;
        if (ctx is null)
            return Task.FromResult(CommandResult<TwoFaCodeVerifyResult>.Fail(Errors.Internal("Missing HttpContext")));
        var email = TokenUtilities.GetSubjectFromToken(ctx);
        if (email is null) return Task.FromResult(CommandResult<TwoFaCodeVerifyResult>.Fail(Errors.Unauthorized()));


        // TODO replace with real TOTP or delivered code
        if (req.Code != "123456")
            return Task.FromResult(CommandResult<TwoFaCodeVerifyResult>.Fail(Errors.Unauthorized()));


        var jwt = TokenUtilities.IssueAccessToken(cfg, email, email);
        return Task.FromResult(CommandResult<TwoFaCodeVerifyResult>.Ok(new TwoFaCodeVerifyResult(jwt)));
    }
}