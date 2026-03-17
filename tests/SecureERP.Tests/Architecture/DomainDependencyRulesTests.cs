using SecureERP.Domain.Exceptions;

namespace SecureERP.Tests.Architecture;

public sealed class DomainDependencyRulesTests
{
    [Fact]
    public void DomainAssembly_ShouldNotReference_ApplicationOrInfrastructure()
    {
        string[] referencedAssemblies = typeof(DomainException)
            .Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToArray();

        Assert.DoesNotContain("SecureERP.Application", referencedAssemblies);
        Assert.DoesNotContain("SecureERP.Infrastructure", referencedAssemblies);
        Assert.DoesNotContain("SecureERP.Api", referencedAssemblies);
    }
}
