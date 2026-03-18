# Purchase Request Module Reference

## Purpose
Referencia oficial para construir nuevos endpoints de negocio usando el contrato de seguridad de SecureERP.

## Endpoint to Permission Map
| Endpoint | Permission | MFA | Notes |
|---|---|---|---|
| `POST /api/v1/purchase/requests` | `PURCHASE.REQUEST.CREATE` | No | Crea cabecera en `DRAFT`. |
| `GET /api/v1/purchase/requests/{id}` | `PURCHASE.REQUEST.READ` | No | Lectura por id con RLS. |
| `GET /api/v1/purchase/requests` | `PURCHASE.REQUEST.READ` | No | Listado visible por contexto. |
| `PUT /api/v1/purchase/requests/{id}` | `PURCHASE.REQUEST.UPDATE` | No | Solo permite `DRAFT`. |
| `PUT /api/v1/purchase/requests/{id}/details` | `PURCHASE.REQUEST.UPDATE` | No | Solo permite `DRAFT`. |
| `POST /api/v1/purchase/requests/{id}/submit` | `PURCHASE.REQUEST.SUBMIT` | No | Cambia `DRAFT -> SUBMITTED`. |
| `POST /api/v1/purchase/requests/{id}/approve` | `PURCHASE.REQUEST.APPROVE` | Yes | `SUBMITTED -> APPROVED`, SoD y MFA. |

## Security Enforcement Location
- Permission + MFA: `[RequirePermission(..., requiresMfa: ...)]` en `src/SecureERP.Api/Modules/Purchase/PurchaseRequestsController.cs`.
- RLS: policy oficial `seguridad.RLS_scope_tenant_empresa` sobre `compras.solicitud` y `compras.solicitud_detalle` en `src/SecureERP.Database/Scripts/IAM/401_phase7_purchase_request_module.sql`.
- SoD (`CREATOR != APPROVER`): `compras.usp_solicitud_aprobar`.
- Auditoria operativa: `cumplimiento.usp_auditoria_operacion_registrar` en SPs create/update/detail/submit/approve.
- Security events: `seguridad.usp_security_event_write` para approve exitoso y denegaciones SoD/MFA.
- Anti-abuso: `IOperationalSecurityService.GuardAsync` en handlers `SubmitPurchaseRequestHandler` y `ApprovePurchaseRequestHandler` con acciones `PURCHASE.REQUEST.SUBMIT` / `PURCHASE.REQUEST.APPROVE`.

## Template Tests for New Modules
- `tests/SecureERP.Tests/Integration/PurchaseRequestModuleIntegrationTests.cs`
  - control de visibilidad por contexto
  - transiciones de estado
  - MFA requerido en endpoint sensible
  - denegacion SoD
  - auditoria y security events
