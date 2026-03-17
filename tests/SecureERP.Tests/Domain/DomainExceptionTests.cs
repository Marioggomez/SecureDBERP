using SecureERP.Domain.Exceptions;

namespace SecureERP.Tests.Domain;

public sealed class DomainExceptionTests
{
    [Fact]
    public void Constructor_ShouldExpose_CodeAndMessage()
    {
        DomainException exception = new("TEST_CODE", "Test message");

        Assert.Equal("TEST_CODE", exception.Code);
        Assert.Equal("Test message", exception.Message);
    }
}
