namespace SecureERP.Application.Modules.Security.Abstractions;

public interface IPasswordHasher
{
    bool VerifyPassword(
        string plainTextPassword,
        byte[] expectedHash,
        byte[] salt,
        string algorithm,
        int iterations);
}
