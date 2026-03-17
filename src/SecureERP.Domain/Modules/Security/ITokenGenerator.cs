namespace SecureERP.Domain.Modules.Security;

public interface ITokenGenerator
{
    string GenerateOpaqueToken(int sizeBytes = 32);

    byte[] ComputeSha256(string opaqueToken);
}
