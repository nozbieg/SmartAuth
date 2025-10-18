using Microsoft.Extensions.Options;
using SmartAuth.Api.Services;
using SmartAuth.Api.Utilities;

namespace SmartAuth.Tests.Tests;

public class MicrosoftAuthenticatorClientTests
{
    [Fact]
    public void Uri_contains_expected_parameters_and_encodings()
    {
        var opts = Options.Create(new TotpOptions { Issuer = "Smart Auth", Digits = 7, Period = 45, Algorithm = "SHA1" });
        var client = new MicrosoftAuthenticatorClient(opts);
        var uri = client.BuildOtpAuthUri("user+tag@example.com", "SECRETBASE32");
        Assert.Contains("otpauth://totp/", uri);
        Assert.Contains("Smart%20Auth", uri); // issuer encoded
        Assert.Contains("user%2Btag%40example.com", uri); // email encoded
        Assert.Contains("secret=SECRETBASE32", uri);
        Assert.Contains("digits=7", uri);
        Assert.Contains("period=45", uri);
        Assert.Contains("issuer=Smart%20Auth", uri);
    }
}

