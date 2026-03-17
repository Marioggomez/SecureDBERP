using SecureERP.Infrastructure.Security;
using System.Security.Cryptography;

namespace SecureERP.Tests.Security;

public sealed class PasswordHasherTests
{
    [Fact]
    public void VerifyPassword_ShouldReturnTrue_ForPbkdf2Sha512()
    {
        PasswordHasher hasher = new();
        byte[] salt = RandomNumberGenerator.GetBytes(32);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2("Password#123", salt, 210000, HashAlgorithmName.SHA512, 64);

        bool isValid = hasher.VerifyPassword("Password#123", hash, salt, "PBKDF2_HMAC_SHA512", 210000);

        Assert.True(isValid);
    }
}
