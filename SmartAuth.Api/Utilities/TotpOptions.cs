namespace SmartAuth.Api.Utilities;

public sealed class TotpOptions
{
    public string Issuer { get; set; } = "SmartAuth";
    public int Digits { get; set; } = 6;
    public int Period { get; set; } = 30;
    public string Algorithm { get; set; } = "SHA1"; // For otpauth URI only; generation uses HMAC-SHA1 currently
}