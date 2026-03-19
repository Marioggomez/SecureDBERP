using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using SecureERP.Api.Modules.Security;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SecureERP.Tests.Architecture;

public sealed class SecurityOperationCatalogCoverageTests
{
    private static readonly Regex OperationTupleRegex = new(
        @"\(\s*N'(?<code>[^']+)'\s*,\s*N'(?<module>[^']+)'\s*,\s*(?<controller>NULL|N'[^']+')\s*,\s*N'(?<action>[^']+)'\s*,\s*N'(?<http>[^']+)'\s*,\s*N'(?<route>[^']+)'\s*,\s*N'[^']*'\s*,\s*(?<permission>NULL|N'[^']+')\s*,\s*(?<requiresAuth>[01])\s*,\s*(?<requiresSession>[01])\s*,\s*(?<requiresCompany>[01])\s*,\s*(?<requiresUnit>[01])\s*,\s*(?<requiresMfa>[01])\s*,\s*(?<requiresAudit>[01])\s*,\s*(?<requiresApproval>[01])\s*,\s*(?<entityCode>NULL|N'[^']+')\s*,\s*(?<active>[01])\s*\)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void ProtectedEndpoints_ShouldBeRegisteredInOfficialOperationCatalog_WithMatchingPermissionAndMfa()
    {
        IReadOnlyList<OperationSeedRow> operations = ParseOperationSeedRows();
        Dictionary<string, OperationSeedRow> byControllerActionHttpRoute = operations
            .GroupBy(op => BuildKey(op.Controller, op.Action, op.HttpMethod, op.Route), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        List<string> missing = [];
        List<string> mismatchedPermission = [];
        List<string> mismatchedMfa = [];
        List<string> operationMissingAction = [];
        List<string> operationMissingPermissionAttribute = [];
        List<string> operationMismatchedPermission = [];
        List<string> operationMismatchedMfa = [];

        Type controllerBaseType = typeof(ControllerBase);
        IEnumerable<Type> controllers = typeof(Program).Assembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && controllerBaseType.IsAssignableFrom(t));

        Dictionary<string, EndpointMetadata> endpointsByKey = new(StringComparer.OrdinalIgnoreCase);
        foreach (Type controller in controllers)
        {
            string controllerRoute = controller.GetCustomAttribute<RouteAttribute>()?.Template ?? string.Empty;
            MethodInfo[] actions = controller
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => m.GetCustomAttributes().Any(a => a is HttpMethodAttribute))
                .ToArray();

            foreach (MethodInfo action in actions)
            {
                RequirePermissionAttribute? requirement = action
                    .GetCustomAttributes<RequirePermissionAttribute>(true)
                    .FirstOrDefault()
                    ?? controller.GetCustomAttributes<RequirePermissionAttribute>(true).FirstOrDefault();

                HttpMethodAttribute httpMethodAttribute = action
                    .GetCustomAttributes()
                    .OfType<HttpMethodAttribute>()
                    .First();

                string httpMethod = httpMethodAttribute.HttpMethods.FirstOrDefault() ?? "GET";
                string actionRoute = httpMethodAttribute.Template ?? string.Empty;
                string normalizedRoute = NormalizeRoute(CombineRoute(controllerRoute, actionRoute));
                string key = BuildKey(controller.Name, action.Name, httpMethod, normalizedRoute);
                endpointsByKey[key] = new EndpointMetadata(controller.Name, action.Name, httpMethod, normalizedRoute, requirement);

                if (requirement is null)
                {
                    continue;
                }

                if (!byControllerActionHttpRoute.TryGetValue(key, out OperationSeedRow? row))
                {
                    missing.Add($"{controller.Name}.{action.Name} [{httpMethod}] {normalizedRoute}");
                    continue;
                }

                if (!string.Equals(row.PermissionCode, requirement.PermissionCode, StringComparison.Ordinal))
                {
                    mismatchedPermission.Add(
                        $"{controller.Name}.{action.Name} -> API:{requirement.PermissionCode} SQL:{row.PermissionCode}");
                }

                if (row.RequiresMfa != requirement.RequiresMfa)
                {
                    mismatchedMfa.Add(
                        $"{controller.Name}.{action.Name} -> API:{requirement.RequiresMfa} SQL:{row.RequiresMfa}");
                }
            }
        }

        foreach (OperationSeedRow operation in operations.Where(static op =>
                     !string.IsNullOrWhiteSpace(op.PermissionCode)
                     && op.Controller.EndsWith("Controller", StringComparison.Ordinal)))
        {
            string key = BuildKey(operation.Controller, operation.Action, operation.HttpMethod, operation.Route);
            if (!endpointsByKey.TryGetValue(key, out EndpointMetadata? endpoint))
            {
                operationMissingAction.Add($"{operation.Controller}.{operation.Action} [{operation.HttpMethod}] {operation.Route}");
                continue;
            }

            RequirePermissionAttribute? requirement = endpoint.Requirement;
            if (requirement is null)
            {
                operationMissingPermissionAttribute.Add($"{endpoint.Controller}.{endpoint.Action} [{endpoint.HttpMethod}] {endpoint.Route}");
                continue;
            }

            if (!string.Equals(requirement.PermissionCode, operation.PermissionCode, StringComparison.Ordinal))
            {
                operationMismatchedPermission.Add(
                    $"{endpoint.Controller}.{endpoint.Action} -> SQL:{operation.PermissionCode} API:{requirement.PermissionCode}");
            }

            if (requirement.RequiresMfa != operation.RequiresMfa)
            {
                operationMismatchedMfa.Add(
                    $"{endpoint.Controller}.{endpoint.Action} -> SQL:{operation.RequiresMfa} API:{requirement.RequiresMfa}");
            }
        }

        Assert.True(missing.Count == 0,
            "Endpoints protegidos sin registro en operacion_api/politica_operacion_api:" + Environment.NewLine
            + string.Join(Environment.NewLine, missing));

        Assert.True(mismatchedPermission.Count == 0,
            "Desalineacion de permiso entre RequirePermission y SQL operativo:" + Environment.NewLine
            + string.Join(Environment.NewLine, mismatchedPermission));

        Assert.True(mismatchedMfa.Count == 0,
            "Desalineacion de requiresMfa entre RequirePermission y SQL operativo:" + Environment.NewLine
            + string.Join(Environment.NewLine, mismatchedMfa));

        Assert.True(operationMissingAction.Count == 0,
            "Operaciones SQL protegidas sin endpoint API correspondiente:" + Environment.NewLine
            + string.Join(Environment.NewLine, operationMissingAction));

        Assert.True(operationMissingPermissionAttribute.Count == 0,
            "Endpoints definidos como protegidos en SQL sin RequirePermission en API:" + Environment.NewLine
            + string.Join(Environment.NewLine, operationMissingPermissionAttribute));

        Assert.True(operationMismatchedPermission.Count == 0,
            "Desalineacion de permiso entre operacion SQL y endpoint API:" + Environment.NewLine
            + string.Join(Environment.NewLine, operationMismatchedPermission));

        Assert.True(operationMismatchedMfa.Count == 0,
            "Desalineacion de requiresMfa entre operacion SQL y endpoint API:" + Environment.NewLine
            + string.Join(Environment.NewLine, operationMismatchedMfa));
    }

    private static IReadOnlyList<OperationSeedRow> ParseOperationSeedRows()
    {
        string repoRoot = GetRepoRoot();
        string[] sqlFiles =
        [
            Path.Combine(repoRoot, "src", "SecureERP.Database", "Scripts", "IAM", "302_security_platform_bootstrap.sql"),
            Path.Combine(repoRoot, "src", "SecureERP.Database", "Scripts", "IAM", "402_phase7_purchase_request_security.sql"),
            Path.Combine(repoRoot, "src", "SecureERP.Database", "Scripts", "IAM", "502_phase8_purchase_order_security.sql")
        ];

        List<OperationSeedRow> rows = [];
        foreach (string file in sqlFiles)
        {
            if (!File.Exists(file))
            {
                continue;
            }

            string sql = File.ReadAllText(file);
            foreach (Match match in OperationTupleRegex.Matches(sql))
            {
                string controllerRaw = match.Groups["controller"].Value;
                string controller = controllerRaw.Equals("NULL", StringComparison.OrdinalIgnoreCase)
                    ? string.Empty
                    : controllerRaw.Replace("N'", string.Empty, StringComparison.Ordinal).TrimEnd('\'');
                string permissionRaw = match.Groups["permission"].Value;
                string permission = permissionRaw.Equals("NULL", StringComparison.OrdinalIgnoreCase)
                    ? string.Empty
                    : permissionRaw.Replace("N'", string.Empty, StringComparison.Ordinal).TrimEnd('\'');

                rows.Add(new OperationSeedRow(
                    match.Groups["code"].Value,
                    controller,
                    match.Groups["action"].Value,
                    match.Groups["http"].Value,
                    NormalizeRoute(match.Groups["route"].Value),
                    permission,
                    match.Groups["requiresMfa"].Value == "1"));
            }
        }

        return rows
            .Where(r =>
                !string.IsNullOrWhiteSpace(r.PermissionCode)
                && !string.IsNullOrWhiteSpace(r.Controller)
                && !string.IsNullOrWhiteSpace(r.Action)
                && r.Controller.EndsWith("Controller", StringComparison.Ordinal))
            .GroupBy(r => BuildKey(r.Controller, r.Action, r.HttpMethod, r.Route), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToArray();
    }

    private static string CombineRoute(string prefix, string action)
    {
        string p = prefix.Trim('/');
        string a = action.Trim('/');
        if (string.IsNullOrWhiteSpace(p))
        {
            return "/" + a;
        }

        if (string.IsNullOrWhiteSpace(a))
        {
            return "/" + p;
        }

        return "/" + p + "/" + a;
    }

    private static string NormalizeRoute(string route)
    {
        string normalized = "/" + route.Trim('/');
        normalized = Regex.Replace(normalized, @"\{([^}:]+):[^}]+\}", "{$1}");
        return normalized;
    }

    private static string BuildKey(string controller, string action, string httpMethod, string route)
        => $"{controller}|{action}|{httpMethod.ToUpperInvariant()}|{NormalizeRoute(route)}";

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

    private sealed record OperationSeedRow(
        string OperationCode,
        string Controller,
        string Action,
        string HttpMethod,
        string Route,
        string PermissionCode,
        bool RequiresMfa);

    private sealed record EndpointMetadata(
        string Controller,
        string Action,
        string HttpMethod,
        string Route,
        RequirePermissionAttribute? Requirement);
}
