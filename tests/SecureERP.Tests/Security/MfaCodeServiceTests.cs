using SecureERP.Infrastructure.Security;
using System.Security.Cryptography;

namespace SecureERP.Tests.Security;

public sealed class MfaCodeServiceTests
{
    [Fact]
    public void GenerateCode_ShouldReturn6Digits()
    {
        MfaCodeService service = new();

        string code = service.GenerateCode();

        Assert.Equal(6, code.Length);
        Assert.True(code.All(char.IsDigit));
    }

    [Fact]
    public void ComputeHash_ShouldBeDeterministic_ForSameInputs()
    {
        MfaCodeService service = new();
        byte[] salt = RandomNumberGenerator.GetBytes(16);

        byte[] hash1 = service.ComputeHash("123456", salt);
        byte[] hash2 = service.ComputeHash("123456", salt);

        Assert.Equal(hash1, hash2);
        Assert.Equal(32, hash1.Length);
    }
}
