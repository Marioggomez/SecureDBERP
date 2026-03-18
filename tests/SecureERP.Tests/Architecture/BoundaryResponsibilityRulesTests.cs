namespace SecureERP.Tests.Architecture;

public sealed class BoundaryResponsibilityRulesTests
{
    [Fact]
    public void ApiLayer_ShouldNotUseSqlClient()
    {
        string apiPath = Path.Combine(GetRepoRoot(), "src", "SecureERP.Api");
        string[] files = GetSourceFiles(apiPath);

        AssertNoPattern(files, "Microsoft.Data.SqlClient");
        AssertNoPattern(files, "SqlConnection");
        AssertNoPattern(files, "SqlCommand");
        AssertNoPattern(files, "SqlParameter");
    }

    [Fact]
    public void ApplicationLayer_ShouldNotUseAspNetCoreTransport()
    {
        string appPath = Path.Combine(GetRepoRoot(), "src", "SecureERP.Application");
        string[] files = GetSourceFiles(appPath);

        AssertNoPattern(files, "Microsoft.AspNetCore");
        AssertNoPattern(files, "HttpContext");
        AssertNoPattern(files, "ControllerBase");
        AssertNoPattern(files, "IActionResult");
    }

    [Fact]
    public void DomainLayer_ShouldNotUseAspNetCoreOrSqlClient()
    {
        string domainPath = Path.Combine(GetRepoRoot(), "src", "SecureERP.Domain");
        string[] files = GetSourceFiles(domainPath);

        AssertNoPattern(files, "Microsoft.AspNetCore");
        AssertNoPattern(files, "HttpContext");
        AssertNoPattern(files, "Microsoft.Data.SqlClient");
        AssertNoPattern(files, "SqlConnection");
    }

    private static string GetRepoRoot()
    {
        DirectoryInfo? current = new(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "SecureERP.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }

    private static string[] GetSourceFiles(string root)
    {
        return Directory
            .GetFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    private static void AssertNoPattern(IEnumerable<string> files, string pattern)
    {
        foreach (string file in files)
        {
            string content = File.ReadAllText(file);
            Assert.DoesNotContain(pattern, content, StringComparison.Ordinal);
        }
    }
}
