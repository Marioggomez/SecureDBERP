using SecureERP.Infrastructure.Security;

namespace SecureERP.Tests.Security;

public sealed class TokenGeneratorTests
{
    [Fact]
    public void GenerateOpaqueToken_ShouldReturnValue()
    {
        TokenGenerator generator = new();

        string token = generator.GenerateOpaqueToken();

        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public void ComputeSha256_ShouldReturn32Bytes()
    {
        TokenGenerator generator = new();

        byte[] hash = generator.ComputeSha256("sample-token");

        Assert.Equal(32, hash.Length);
    }
}
