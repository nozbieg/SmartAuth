namespace SmartAuth.Infrastructure.Security;

using System.Security.Cryptography;

public static class Passwords
{
    private const int SaltSize = 16; // 128 bit
    private const int KeySize = 32; // 256 bit
    private const int Iterations = 100_000;
    private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA256;

    public static (byte[] Hash, byte[] Salt) Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithm,
            KeySize);
        return (hash, salt);
    }

    public static bool Verify(string password, byte[] hash, byte[] salt)
    {
        var newHash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithm,
            KeySize);
        return CryptographicOperations.FixedTimeEquals(newHash, hash);
    }

    public static string ToBase64(byte[] data) => Convert.ToBase64String(data);
    public static byte[] FromBase64(string base64) => Convert.FromBase64String(base64);
}