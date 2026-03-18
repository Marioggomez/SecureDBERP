using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using SecureERP.Api.Modules.Security;
using SecureERP.Application.Modules.Security;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SecureERP.Tests.Architecture;

public sealed class SecurityGuardrailEnforcementTests
{
    private static readonly Regex PermissionLiteralRegex = new(
        "\"([A-Z][A-Z0-9_]*\\.[A-Z][A-Z0-9_]*\\.[A-Z][A-Z0-9_]*)\"",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex PermissionSeedRegex = new(
        "\\(N'(?<code>[A-Z0-9_\\.]+)'\\s*,\\s*N'[^']*'\\s*,\\s*N'[^']*'\\s*,\\s*N'[^']*'\\s*,\\s*N'[^']*'\\s*,\\s*N'[^']*'\\s*,\\s*(?<requiresMfa>[01])\\s*,\\s*(?<sensitive>[01])\\s*,\\s*[01]\\)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex PermissionDocRegex = new(
        "^\\|\\s*`(?<code>[A-Z][A-Z0-9_]*\\.[A-Z][A-Z0-9_]*\\.[A-Z][A-Z0-9_]*)`\\s*\\|",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);

    private static readonly Regex HttpContractTypeRegex = new(
        "\\b(class|record)\\s+\\w+(RequestContract|ResponseContract|Contract)\\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void PermissionCodes_ShouldNotBeHardcodedOutsidePermissionsClass()
    {
        string repoRoot = GetRepoRoot();
        string[] files = GetSourceFiles(Path.Combine(repoRoot, "src"));
        List<string> violations = [];

        foreach (string file in files)
        {
            if (file.EndsWith(Path.Combine("SecureERP.Application", "Modules", "Security", "Permissions.cs"), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string content = File.ReadAllText(file);
            MatchCollection matches = PermissionLiteralRegex.Matches(content);
            foreach (Match match in matches)
            {
                violations.Add($"{GetRepoRelativePath(repoRoot, file)} -> {match.Groups[1].Value}");
            }
        }

        Assert.True(violations.Count == 0, "Permission hardcoded literals detected outside Permissions class:" + Environment.NewLine + string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void NonPublicControllerEndpoints_ShouldDeclareRequirePermission()
    {
        List<string> violations = [];
        Type controllerBaseType = typeof(ControllerBase);
        Type requirePermissionType = typeof(RequirePermissionAttribute);

        IEnumerable<Type> controllerTypes = typeof(Program).Assembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && controllerBaseType.IsAssignableFrom(t));

        foreach (Type controllerType in controllerTypes)
        {
            string routePrefix = controllerType.GetCustomAttribute<RouteAttribute>()?.Template ?? string.Empty;
            bool isPublicController = routePrefix.StartsWith("api/v1/auth", StringComparison.OrdinalIgnoreCase);

            MethodInfo[] actions = controllerType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => m.GetCustomAttributes().Any(a => a is HttpMethodAttribute))
                .ToArray();

            foreach (MethodInfo action in actions)
            {
                bool isAnonymous = action.GetCustomAttributes()
                    .Any(a => a.GetType().Name.Equals("AllowAnonymousAttribute", StringComparison.Ordinal));
                if (isPublicController || isAnonymous)
                {
                    continue;
                }

                bool hasPermission = action.GetCustomAttributes(requirePermissionType, true).Any()
                    || controllerType.GetCustomAttributes(requirePermissionType, true).Any();
                if (!hasPermission)
                {
                    violations.Add($"{controllerType.FullName}.{action.Name}");
                }
            }
        }

        Assert.True(violations.Count == 0, "Protected endpoints without RequirePermission detected:" + Environment.NewLine + string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void EndpointsUsingMfaRequiredPermissions_ShouldSetRequiresMfaTrue()
    {
        HashSet<string> requiresMfaPermissions = ParsePermissionMetadataFromSeed()
            .Where(p => p.RequiresMfa)
            .Select(p => p.Code)
            .ToHashSet(StringComparer.Ordinal);

        List<string> violations = [];
        Type controllerBaseType = typeof(ControllerBase);

        IEnumerable<Type> controllerTypes = typeof(Program).Assembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && controllerBaseType.IsAssignableFrom(t));

        foreach (Type controllerType in controllerTypes)
        {
            foreach (MethodInfo action in controllerType
                         .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                         .Where(m => m.GetCustomAttributes().Any(a => a is HttpMethodAttribute)))
            {
                RequirePermissionAttribute? permissionAttribute = action
                    .GetCustomAttributes<RequirePermissionAttribute>(true)
                    .FirstOrDefault()
                    ?? controllerType.GetCustomAttributes<RequirePermissionAttribute>(true).FirstOrDefault();

                if (permissionAttribute is null)
                {
                    continue;
                }

                if (requiresMfaPermissions.Contains(permissionAttribute.PermissionCode) && !permissionAttribute.RequiresMfa)
                {
                    violations.Add($"{controllerType.FullName}.{action.Name} -> {permissionAttribute.PermissionCode}");
                }
            }
        }

        Assert.True(violations.Count == 0, "Endpoints with MFA-required permissions must declare requiresMfa=true:" + Environment.NewLine + string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void PermissionsRuntimeConstants_ShouldMatchOfficialSqlSeedCatalog()
    {
        HashSet<string> runtime = Permissions.All.ToHashSet(StringComparer.Ordinal);
        HashSet<string> sqlSeed = ParsePermissionMetadataFromSeed()
            .Select(p => p.Code)
            .ToHashSet(StringComparer.Ordinal);

        Assert.True(runtime.SetEquals(sqlSeed), BuildSetMismatchMessage("Permissions runtime constants", runtime, "Official SQL seed catalog", sqlSeed));
    }

    [Fact]
    public void PermissionsDocumentation_ShouldMatchRuntimePermissions()
    {
        string repoRoot = GetRepoRoot();
        string docPath = Path.Combine(repoRoot, "docs", "security", "permissions-catalog.md");
        string content = File.ReadAllText(docPath);
        HashSet<string> documented = PermissionDocRegex
            .Matches(content)
            .Select(m => m.Groups["code"].Value)
            .ToHashSet(StringComparer.Ordinal);
        HashSet<string> runtime = Permissions.All.ToHashSet(StringComparer.Ordinal);

        Assert.True(runtime.SetEquals(documented), BuildSetMismatchMessage("Permissions runtime constants", runtime, "Permissions documentation", documented));
    }

    [Fact]
    public void HttpContracts_ShouldOnlyExistInsideApiModules()
    {
        string repoRoot = GetRepoRoot();
        string srcRoot = Path.Combine(repoRoot, "src");
        string[] files = GetSourceFiles(srcRoot);
        List<string> violations = [];

        foreach (string file in files)
        {
            bool isApiModulesPath = file.Contains(Path.Combine("src", "SecureERP.Api", "Modules"), StringComparison.OrdinalIgnoreCase);
            if (isApiModulesPath)
            {
                continue;
            }

            string content = File.ReadAllText(file);
            if (HttpContractTypeRegex.IsMatch(content))
            {
                violations.Add(GetRepoRelativePath(repoRoot, file));
            }
        }

        Assert.True(violations.Count == 0, "HTTP contracts detected outside SecureERP.Api.Modules:" + Environment.NewLine + string.Join(Environment.NewLine, violations));
    }

    private static IReadOnlyList<PermissionSeedRow> ParsePermissionMetadataFromSeed()
    {
        string repoRoot = GetRepoRoot();
        string sqlPath = Path.Combine(repoRoot, "src", "SecureERP.Database", "Scripts", "IAM", "301_security_platform_permissions_catalog.sql");
        string sqlContent = File.ReadAllText(sqlPath);

        List<PermissionSeedRow> rows = [];
        foreach (Match match in PermissionSeedRegex.Matches(sqlContent))
        {
            rows.Add(new PermissionSeedRow(
                match.Groups["code"].Value,
                match.Groups["requiresMfa"].Value == "1",
                match.Groups["sensitive"].Value == "1"));
        }

        return rows
            .GroupBy(r => r.Code, StringComparer.Ordinal)
            .Select(g => g.First())
            .OrderBy(r => r.Code, StringComparer.Ordinal)
            .ToArray();
    }

    private static string BuildSetMismatchMessage(string leftName, HashSet<string> left, string rightName, HashSet<string> right)
    {
        string[] missingInRight = left.Except(right, StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        string[] missingInLeft = right.Except(left, StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray();

        return $"{leftName} and {rightName} are not aligned." + Environment.NewLine
            + $"Only in {leftName}: {string.Join(", ", missingInRight)}" + Environment.NewLine
            + $"Only in {rightName}: {string.Join(", ", missingInLeft)}";
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

    private static string GetRepoRelativePath(string repoRoot, string fullPath)
        => Path.GetRelativePath(repoRoot, fullPath).Replace('\\', '/');

    private sealed record PermissionSeedRow(string Code, bool RequiresMfa, bool Sensitive);
}
