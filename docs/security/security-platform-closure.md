# SecureERP Security Platform Closure

## Scope
Cierre formal del modulo de seguridad para desarrollo reutilizable:
- contrato oficial de permisos
- bootstrap oficial
- clasificacion oficial de endpoints
- politica oficial MFA
- politica oficial auditoria y security events
- estandar oficial de endpoint seguro

## Artifacts
- SQL release: `src/SecureERP.Database/Scripts/IAM/v6_security_platform_closure_release.sql`
- SQL permissions: `src/SecureERP.Database/Scripts/IAM/301_security_platform_permissions_catalog.sql`
- SQL bootstrap: `src/SecureERP.Database/Scripts/IAM/302_security_platform_bootstrap.sql`
- Permissions class: `src/SecureERP.Application/Modules/Security/Permissions.cs`
- Endpoint standard: `docs/security/secure-endpoint-standard.md`
- Permissions catalog: `docs/security/permissions-catalog.md`

## Bootstrap Result
`302_security_platform_bootstrap.sql` deja:
1. tenant bootstrap (`BOOTSTRAP`)
2. empresa bootstrap (`MAIN`)
3. usuario bootstrap admin (`admin@secureerp.local`)
4. rol base (`SECURITY.ADMIN`)
5. asignacion de rol al admin bootstrap
6. credencial local admin con PBKDF2-HMAC-SHA512
7. politica base de tenant (password, lockout, MFA)
8. politica base operacional anti-abuso (login, mfa, validate-session, workflow create)
9. politica IP base (template desactivado para loopback)
10. operaciones API + politica_operacion_api inicial
11. excepciones ALLOW bootstrap para permisos activos

## Endpoint Classification Matrix
| Category | Auth | Session | Permission | MFA | Rate Limit | Audit | Security Event |
|---|---|---|---|---|---|---|---|
| Public | No | No | No | No | Yes (if configured) | Optional | Optional |
| Authenticated | Yes | Yes | No | No | Yes | Optional | Optional |
| Authorized | Yes | Yes | Yes | No | Yes | Yes | Optional |
| Sensitive MFA | Yes | Yes | Yes | Yes | Yes (strict) | Yes | Yes |
| Sensitive Reinforced | Yes | Yes | Yes | Optional by policy | Yes (strict + lockout/IP) | Yes | Yes |
| Privileged Admin | Yes | Yes | Yes (admin permission) | Yes | Yes (strict) | Yes | Yes |

## Official MFA Policy
MFA obligatorio para:
- `WORKFLOW.APPROVAL_INSTANCE.CREATE`
- `SECURITY.USER.RESET_PASSWORD`
- `SECURITY.ROLE.ASSIGN`
- `AUTH.SESSION.REVOKE`
- `AUDIT.SECURITY_EVENT.READ`

MFA no obligatorio por defecto para:
- lecturas operativas no sensibles (`ORGANIZATION.UNIT.READ`, `WORKFLOW.APPROVAL_INSTANCE.READ`, `SYSTEM.HEALTH.READ`)
- emision/verificacion MFA de login (`AUTH.MFA.CHALLENGE`, `AUTH.MFA.VERIFY`)

Implementacion oficial:
- decorador endpoint: `[RequirePermission(<PERMISSION_CODE>, requiresMfa: true|false)]`
- evaluacion central: `AuthorizationEvaluator` + SP de autorizacion

## Official Audit and Security Event Policy
| Operation Type | Audit Authorization | Security Event |
|---|---|---|
| Denied by permission/scope/mfa | Yes | Yes (`DENIED`) |
| Login/MFA/session lifecycle | Optional | Yes |
| Sensitive write | Yes | Yes |
| Non-sensitive read | Optional | Optional |

Severity rule:
- `INFO`: success flow lifecycle
- `WARNING`: denied attempts, abuse signals
- `ERROR`: unhandled failures

Mandatory context:
- `correlationId`
- `tenantId`
- `companyId` (if available)
- `userId` (if available)
- endpoint/action code

## Non-Negotiable Rules for New APIs
1. No endpoint without official permission code.
2. No sensitive endpoint without explicit MFA decision.
3. No write endpoint without explicit audit decision.
4. No bypass of session/middleware/authorization evaluator.
5. No custom ad-hoc permission names outside official catalog.
