using System.Security.Cryptography;

namespace SmartAuth.Api.Utilities;

public static class AuthCrypto
{
    private const int SaltSize = 16; 
    private const int KeySize = 32; 
    private const int Iterations = 100_000; 
    private static readonly HashAlgorithmName Algo = HashAlgorithmName.SHA256;

    public static (byte[] hash, byte[] salt) HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algo, KeySize);
        return (hash, salt);
    }

    public static bool VerifyPassword(string password, byte[] storedHash, byte[] storedSalt)
    {
        if (storedHash is null || storedHash.Length != KeySize) return false;
        if (storedSalt is null || storedSalt.Length != SaltSize) return false;

        byte[] computed = Rfc2898DeriveBytes.Pbkdf2(password, storedSalt, Iterations, Algo, KeySize);
        return CryptographicOperations.FixedTimeEquals(computed, storedHash);
    }
}