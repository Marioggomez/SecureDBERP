using SecureERP.Domain.Modules.Security;
using System.Security.Cryptography;
using System.Text;

namespace SecureERP.Infrastructure.Security;

public sealed class MfaCodeService : IMfaCodeService
{
    public string GenerateCode(int digits = 6)
    {
        if (digits < 4 || digits > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(digits), "MFA code digits must be between 4 and 10.");
        }

        int min = (int)Math.Pow(10, digits - 1);
        int max = (int)Math.Pow(10, digits) - 1;
        int value = RandomNumberGenerator.GetInt32(min, max + 1);
        return value.ToString($"D{digits}");
    }

    public byte[] GenerateSalt(int sizeBytes = 16)
    {
        if (sizeBytes < 8)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Salt size must be at least 8 bytes.");
        }

        return RandomNumberGenerator.GetBytes(sizeBytes);
    }

    public byte[] ComputeHash(string otpCode, byte[] salt)
    {
        byte[] codeBytes = Encoding.UTF8.GetBytes(otpCode);
        byte[] payload = new byte[salt.Length + codeBytes.Length];
        Buffer.BlockCopy(salt, 0, payload, 0, salt.Length);
        Buffer.BlockCopy(codeBytes, 0, payload, salt.Length, codeBytes.Length);
        return SHA256.HashData(payload);
    }
}
