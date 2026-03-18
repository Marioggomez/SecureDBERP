# SecureERP Official Permissions Catalog

## Purpose
Catalogo oficial de permisos para desarrollo de endpoints y bootstrap de seguridad.

## Convention
`MODULO.ENTIDAD.ACCION`

## Source of Truth
- SQL catalog: `seguridad.catalogo_permiso_oficial`
- Runtime constants: `SecureERP.Application.Modules.Security.Permissions`

## Official Permissions
| Code | Module | Entity | Action | Technical Description | Functional Description | Requires MFA | Sensitive | Active |
|---|---|---|---|---|---|---|---|---|
| `AUTH.SESSION.VALIDATE` | `AUTH` | `SESSION` | `VALIDATE` | Validates opaque session token from API and middleware. | Validate active session. | No | No | Yes |
| `AUTH.SESSION.REVOKE` | `AUTH` | `SESSION` | `REVOKE` | Revokes user session in current scope. | Revoke active session. | Yes | Yes | Yes |
| `AUTH.MFA.CHALLENGE` | `AUTH` | `MFA` | `CHALLENGE` | Issues MFA challenge for login or step-up. | Generate MFA challenge. | No | Yes | Yes |
| `AUTH.MFA.VERIFY` | `AUTH` | `MFA` | `VERIFY` | Verifies MFA challenge and updates state. | Verify MFA code. | No | Yes | Yes |
| `SECURITY.USER.READ` | `SECURITY` | `USER` | `READ` | Reads security users by scoped context. | Read users. | No | Yes | Yes |
| `SECURITY.USER.RESET_PASSWORD` | `SECURITY` | `USER` | `RESET_PASSWORD` | Starts/confirms password reset operation. | Reset user password. | Yes | Yes | Yes |
| `SECURITY.ROLE.ASSIGN` | `SECURITY` | `ROLE` | `ASSIGN` | Assigns roles and scopes to users. | Assign role. | Yes | Yes | Yes |
| `ORGANIZATION.UNIT.READ` | `ORGANIZATION` | `UNIT` | `READ` | Lists organization units with scoped visibility. | Read organization units. | No | No | Yes |
| `ORGANIZATION.UNIT.CREATE` | `ORGANIZATION` | `UNIT` | `CREATE` | Creates organization unit in active tenant/company. | Create organization unit. | No | Yes | Yes |
| `WORKFLOW.APPROVAL_INSTANCE.READ` | `WORKFLOW` | `APPROVAL_INSTANCE` | `READ` | Lists approval instances with RLS and authorization. | Read approval instances. | No | No | Yes |
| `WORKFLOW.APPROVAL_INSTANCE.CREATE` | `WORKFLOW` | `APPROVAL_INSTANCE` | `CREATE` | Creates sensitive approval instance. | Create approval instance. | Yes | Yes | Yes |
| `PURCHASE.REQUEST.READ` | `PURCHASE` | `REQUEST` | `READ` | Lists and reads purchase requests by tenant/company scope. | Read purchase requests. | No | No | Yes |
| `PURCHASE.REQUEST.CREATE` | `PURCHASE` | `REQUEST` | `CREATE` | Creates draft purchase request header. | Create purchase request. | No | Yes | Yes |
| `PURCHASE.REQUEST.UPDATE` | `PURCHASE` | `REQUEST` | `UPDATE` | Updates draft purchase request header and details. | Update draft purchase request. | No | Yes | Yes |
| `PURCHASE.REQUEST.SUBMIT` | `PURCHASE` | `REQUEST` | `SUBMIT` | Submits draft purchase request to approval state. | Submit purchase request. | No | Yes | Yes |
| `PURCHASE.REQUEST.APPROVE` | `PURCHASE` | `REQUEST` | `APPROVE` | Approves submitted purchase request with MFA and SoD enforcement. | Approve purchase request. | Yes | Yes | Yes |
| `PURCHASE.ORDER.READ` | `PURCHASE` | `ORDER` | `READ` | Lists and reads purchase orders by tenant/company scope. | Read purchase orders. | No | No | Yes |
| `PURCHASE.ORDER.CREATE` | `PURCHASE` | `ORDER` | `CREATE` | Creates draft purchase order header. | Create purchase order. | No | Yes | Yes |
| `PURCHASE.ORDER.UPDATE` | `PURCHASE` | `ORDER` | `UPDATE` | Updates draft purchase order header and details. | Update draft purchase order. | No | Yes | Yes |
| `PURCHASE.ORDER.SUBMIT` | `PURCHASE` | `ORDER` | `SUBMIT` | Submits draft purchase order to approval state. | Submit purchase order. | No | Yes | Yes |
| `PURCHASE.ORDER.APPROVE` | `PURCHASE` | `ORDER` | `APPROVE` | Approves submitted purchase order with MFA and SoD enforcement. | Approve purchase order. | Yes | Yes | Yes |
| `SYSTEM.HEALTH.READ` | `SYSTEM` | `HEALTH` | `READ` | Reads API health endpoints. | Read platform health. | No | No | Yes |
| `AUDIT.SECURITY_EVENT.READ` | `AUDIT` | `SECURITY_EVENT` | `READ` | Reads security and observability events. | Read security events. | Yes | Yes | Yes |

## Compatibility Rule
Permisos existentes (`ORGANIZATION.UNIT.*`, `WORKFLOW.APPROVAL_INSTANCE.*`) se mantienen sin renombrar para no romper compatibilidad.
