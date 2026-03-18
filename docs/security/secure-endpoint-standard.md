# Secure Endpoint Standard (Official)

## Mandatory Build Checklist Per Endpoint
1. Route and naming defined.
2. Request/response contracts explicit.
3. Permission code selected from `Permissions`.
4. MFA decision explicit in `RequirePermission`.
5. Application handler created.
6. Infrastructure repository method created.
7. Stored procedure selected/reused (or approved new one).
8. Audit/security events decision documented.
9. RLS and `SESSION_CONTEXT` impact validated.
10. Anti-abuse/rate limit requirement validated.
11. Tests added (authz + RLS + anti-abuse when required).

## Endpoint Types and Required Controls
| Endpoint Type | RequirePermission | requiresMfa | Required Tests |
|---|---|---|---|
| Read protected | Yes | Usually No | 401/403 + scope/RLS read |
| Write protected | Yes | Policy-driven | 401/403 + write validation + audit |
| Sensitive write | Yes | Yes | 401/403 + MFA-deny + success with MFA |
| Public auth flow | No | N/A | anti-abuse + uniform errors |

## Official Templates

### 1. Read Endpoint Template
```csharp
[HttpGet]
[RequirePermission(Permissions.OrganizationUnitRead)]
public async Task<ActionResult<IReadOnlyList<OrganizationUnitContract>>> List(CancellationToken cancellationToken)
{
    var data = await _listHandler.HandleAsync(cancellationToken);
    return Ok(data.Select(Map).ToList());
}
```

### 2. Write Endpoint Template
```csharp
[HttpPost]
[RequirePermission(Permissions.OrganizationUnitCreate)]
public async Task<ActionResult<CreateOrganizationUnitResponseContract>> Create(
    [FromBody] CreateOrganizationUnitRequestContract request,
    CancellationToken cancellationToken)
{
    var result = await _createHandler.HandleAsync(Map(request), cancellationToken);
    return result.IsSuccess ? Ok(Map(result)) : BadRequest(Map(result));
}
```

### 3. Sensitive MFA Endpoint Template
```csharp
[HttpPost]
[RequirePermission(Permissions.WorkflowApprovalInstanceCreate, true)]
public async Task<ActionResult<CreateApprovalInstanceResponseContract>> Create(
    [FromBody] CreateApprovalInstanceRequestContract request,
    CancellationToken cancellationToken)
{
    var result = await _createHandler.HandleAsync(Map(request), cancellationToken);
    return result.IsSuccess ? Ok(Map(result)) : BadRequest(Map(result));
}
```

### 4. Protected Endpoint Test Template
```csharp
[Fact]
public async Task ProtectedEndpoint_WithoutPermission_ShouldReturnForbidden()
{
    // Arrange valid session + missing permission
    // Act endpoint
    // Assert 403 + AUTHZ_DENIED
}
```

### 5. RLS Test Template
```csharp
[Fact]
public async Task ReadEndpoint_ShouldNotReturnRowsFromOtherCompany()
{
    // Arrange data in company A and B
    // Session context company A
    // Assert only company A rows returned
}
```

### 6. Anti-Abuse Test Template
```csharp
[Fact]
public async Task SensitiveEndpoint_ShouldRateLimitAfterThreshold()
{
    // Arrange policy threshold
    // Act repeated calls
    // Assert rejection code AUTH_REQUEST_REJECTED
}
```

## Official Real References in Repo
- Read endpoint: `src/SecureERP.Api/Modules/Organization/OrganizationUnitsController.cs`
- Write endpoint: `src/SecureERP.Api/Modules/Organization/OrganizationUnitsController.cs`
- Sensitive MFA endpoint: `src/SecureERP.Api/Modules/Workflow/ApprovalInstancesController.cs`
- End-to-end business module reference: `src/SecureERP.Api/Modules/Purchase/PurchaseRequestsController.cs`
- Security middleware: `src/SecureERP.Api/Middleware/SecurityContextMiddleware.cs`
- Anti-abuse tests: `tests/SecureERP.Tests/Integration/OperationalSecurityPhase5aIntegrationTests.cs`
- RLS/integration tests: `tests/SecureERP.Tests/Integration/BusinessPilotPhase4IntegrationTests.cs`
- Purchase request tests template: `tests/SecureERP.Tests/Integration/PurchaseRequestModuleIntegrationTests.cs`
