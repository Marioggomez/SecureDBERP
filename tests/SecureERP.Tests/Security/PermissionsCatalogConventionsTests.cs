using SecureERP.Application.Modules.Security;
using System.Text.RegularExpressions;

namespace SecureERP.Tests.Security;

public sealed class PermissionsCatalogConventionsTests
{
    private static readonly Regex PermissionCodePattern = new(
        @"^[A-Z][A-Z0-9_]*\.[A-Z][A-Z0-9_]*\.[A-Z][A-Z0-9_]*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void AllPermissions_ShouldBeUnique()
    {
        Assert.Equal(Permissions.All.Count, Permissions.All.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void AllPermissions_ShouldMatchOfficialConvention()
    {
        foreach (string code in Permissions.All)
        {
            Assert.Matches(PermissionCodePattern, code);
        }
    }
}
