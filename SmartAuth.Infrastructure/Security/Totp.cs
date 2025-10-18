using System.Security.Cryptography;
using System.Text;

namespace SmartAuth.Infrastructure.Security;

public static class Totp
{
    // RFC 6238 defaults: 30s step, 6 digits, SHA1
    public static string GenerateCode(string base32Secret, DateTimeOffset? timestamp = null, int stepSeconds = 30, int digits = 6)
    {
        var time = timestamp ?? DateTimeOffset.UtcNow;
        var counter = (long)Math.Floor(time.ToUnixTimeSeconds() / (double)stepSeconds);
        var key = Base32Decode(base32Secret);

        Span<byte> counterBytes = stackalloc byte[8];
        for (var i = 7; i >= 0; i--)
        {
            counterBytes[i] = (byte)(counter & 0xFF);
            counter >>= 8;
        }

        using var hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(counterBytes.ToArray());
        var offset = hash[^1] & 0x0F;
        var binaryCode = ((hash[offset] & 0x7F) << 24) |
                         ((hash[offset + 1] & 0xFF) << 16) |
                         ((hash[offset + 2] & 0xFF) << 8) |
                         (hash[offset + 3] & 0xFF);
        var otp = binaryCode % (int)Math.Pow(10, digits);
        return otp.ToString(new string('0', digits));
    }

    public static bool ValidateCode(string base32Secret, string code, DateTimeOffset? timestamp = null, int stepSeconds = 30, int digits = 6, int allowedPastSteps = 1, int allowedFutureSteps = 1)
    {
        var time = timestamp ?? DateTimeOffset.UtcNow;
        for (int i = -allowedPastSteps; i <= allowedFutureSteps; i++)
        {
            var t = time.AddSeconds(i * stepSeconds);
            if (GenerateCode(base32Secret, t, stepSeconds, digits) == code)
                return true;
        }
        return false;
    }

    public static string GenerateSecret(int numBytes = 20)
    {
        Span<byte> data = stackalloc byte[numBytes];
        RandomNumberGenerator.Fill(data);
        return Base32Encode(data);
    }

    private static byte[] Base32Decode(string input)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var cleaned = input.Trim().Replace("=", string.Empty).ToUpperInvariant();
        var bits = 0;
        var value = 0;
        var bytes = new List<byte>();
        foreach (var c in cleaned)
        {
            var idx = alphabet.IndexOf(c);
            if (idx < 0) throw new FormatException("Invalid base32 character.");
            value = (value << 5) | idx;
            bits += 5;
            if (bits >= 8)
            {
                bits -= 8;
                bytes.Add((byte)(value >> bits));
                value &= (1 << bits) - 1;
            }
        }
        return bytes.ToArray();
    }

    private static string Base32Encode(ReadOnlySpan<byte> data)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var outputLength = (int)Math.Ceiling(data.Length / 5d) * 8;
        var sb = new StringBuilder(outputLength);
        int bits = 0;
        int value = 0;
        foreach (var b in data)
        {
            value = (value << 8) | b;
            bits += 8;
            while (bits >= 5)
            {
                bits -= 5;
                sb.Append(alphabet[(value >> bits) & 0x1F]);
                value &= (1 << bits) - 1;
            }
        }
        if (bits > 0)
        {
            sb.Append(alphabet[(value << (5 - bits)) & 0x1F]);
        }
        while (sb.Length % 8 != 0) sb.Append('=');
        return sb.ToString();
    }
}

