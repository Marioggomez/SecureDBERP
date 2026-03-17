using SecureERP.Domain.Modules.Security;
using System.Security.Cryptography;
using System.Text;

namespace SecureERP.Infrastructure.Security;

public sealed class TokenGenerator : ITokenGenerator
{
    public string GenerateOpaqueToken(int sizeBytes = 32)
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(sizeBytes));
    }

    public byte[] ComputeSha256(string opaqueToken)
    {
        return SHA256.HashData(Encoding.UTF8.GetBytes(opaqueToken));
    }
}
