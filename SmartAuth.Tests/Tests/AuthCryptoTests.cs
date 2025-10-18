using SmartAuth.Api.Utilities;

namespace SmartAuth.Tests.Tests;

public class AuthCryptoTests
{
    [Fact]
    public void Hash_and_verify_roundtrip_success()
    {
        var (hash, salt) = AuthCrypto.HashPassword("Sup3rSecret!");
        Assert.NotNull(hash);
        Assert.NotNull(salt);
        Assert.True(AuthCrypto.VerifyPassword("Sup3rSecret!", hash, salt));
        Assert.False(AuthCrypto.VerifyPassword("Wrong", hash, salt));
    }

    [Fact]
    public void Verify_fails_for_wrong_lengths()
    {
        var (hash, salt) = AuthCrypto.HashPassword("Password1!");
        var badHash = hash.Take(10).ToArray(); // wrong size
        var badSalt = salt.Take(5).ToArray(); // wrong size
        Assert.False(AuthCrypto.VerifyPassword("Password1!", badHash, salt));
        Assert.False(AuthCrypto.VerifyPassword("Password1!", hash, badSalt));
    }
}

