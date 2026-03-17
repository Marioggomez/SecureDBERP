namespace SecureERP.Domain.Modules.Security;

public interface IPasswordHasher
{
    bool VerifyPassword(
        string plainTextPassword,
        byte[] expectedHash,
        byte[] salt,
        string algorithm,
        int iterations);
}
