namespace SecureERP.Domain.Modules.Security;

public interface IMfaCodeService
{
    string GenerateCode(int digits = 6);

    byte[] GenerateSalt(int sizeBytes = 16);

    byte[] ComputeHash(string otpCode, byte[] salt);
}
