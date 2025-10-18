using Microsoft.Extensions.Options;
using SmartAuth.Api.Utilities;

namespace SmartAuth.Api.Services;

public interface IMicrosoftAuthenticatorClient
{
    string BuildOtpAuthUri(string email, string secret);
}

public sealed class MicrosoftAuthenticatorClient(IOptions<TotpOptions> opts) : IMicrosoftAuthenticatorClient
{
    private readonly TotpOptions _opts = opts.Value;
    public string BuildOtpAuthUri(string email, string secret)
    {
        var issuerEsc = Uri.EscapeDataString(_opts.Issuer);
        var emailEsc = Uri.EscapeDataString(email);
        return $"otpauth://totp/{issuerEsc}:{emailEsc}?secret={secret}&issuer={issuerEsc}&algorithm={_opts.Algorithm}&digits={_opts.Digits}&period={_opts.Period}";
    }
}


