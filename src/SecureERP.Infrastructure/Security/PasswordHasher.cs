using SecureERP.Domain.Modules.Security;
using System.Security.Cryptography;

namespace SecureERP.Infrastructure.Security;

public sealed class PasswordHasher : IPasswordHasher
{
    public bool VerifyPassword(
        string plainTextPassword,
        byte[] expectedHash,
        byte[] salt,
        string algorithm,
        int iterations)
    {
        if (string.IsNullOrWhiteSpace(plainTextPassword) ||
            expectedHash.Length == 0 ||
            salt.Length == 0 ||
            string.IsNullOrWhiteSpace(algorithm))
        {
            return false;
        }

        int effectiveIterations = iterations > 0 ? iterations : 100_000;
        byte[] computedHash = algorithm.Trim().ToUpperInvariant() switch
        {
            "PBKDF2_SHA512" => Pbkdf2(plainTextPassword, salt, effectiveIterations, HashAlgorithmName.SHA512, expectedHash.Length),
            "PBKDF2-SHA512" => Pbkdf2(plainTextPassword, salt, effectiveIterations, HashAlgorithmName.SHA512, expectedHash.Length),
            "PBKDF2_HMAC_SHA512" => Pbkdf2(plainTextPassword, salt, effectiveIterations, HashAlgorithmName.SHA512, expectedHash.Length),
            "PBKDF2_SHA256" => Pbkdf2(plainTextPassword, salt, effectiveIterations, HashAlgorithmName.SHA256, expectedHash.Length),
            "PBKDF2-SHA256" => Pbkdf2(plainTextPassword, salt, effectiveIterations, HashAlgorithmName.SHA256, expectedHash.Length),
            "PBKDF2" => Pbkdf2(plainTextPassword, salt, effectiveIterations, HashAlgorithmName.SHA256, expectedHash.Length),
            _ => Array.Empty<byte>()
        };

        return computedHash.Length > 0 && CryptographicOperations.FixedTimeEquals(computedHash, expectedHash);
    }

    private static byte[] Pbkdf2(
        string password,
        byte[] salt,
        int iterations,
        HashAlgorithmName hashAlgorithm,
        int outputLength)
    {
        return Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, hashAlgorithm, outputLength);
    }
}
