# SecureERP Definition of Done (DoD) - Endpoints y Modulos

## Proposito
Regla operativa obligatoria para liberar endpoints y modulos sin romper el contrato oficial de seguridad y arquitectura.

## Regla de bloqueo
Un endpoint o modulo no se considera terminado si al menos un item obligatorio queda en "No".

## A. DoD obligatorio para endpoint nuevo o modificado

### 1. Permisos y autorizacion
- [ ] El endpoint usa un permiso oficial con formato `MODULO.ENTIDAD.ACCION`.
- [ ] Si el permiso es nuevo, existe en `seguridad.catalogo_permiso_oficial` (seed SQL).
- [ ] Si el permiso es nuevo, existe constante en `SecureERP.Application.Modules.Security.Permissions`.
- [ ] El endpoint aplica `[RequirePermission(Permissions.X, requiresMfa: ...)]`.
- [ ] No existen permisos hardcodeados fuera de `Permissions`.

### 2. Clasificacion del endpoint
- [ ] El endpoint esta clasificado como: publico, autenticado, autorizado, sensible con MFA, sensible con anti-abuso reforzado o administrativo.
- [ ] La clasificacion esta alineada a `docs/security/secure-endpoint-standard.md`.

### 3. MFA
- [ ] Se tomo decision explicita: `requiresMfa=true` o `requiresMfa=false`.
- [ ] Si el permiso esta marcado como `requiere_mfa=1` en catalogo oficial, el endpoint declara `requiresMfa=true`.
- [ ] La decision de MFA esta documentada en el PR.

### 4. Auditoria y security events
- [ ] Se tomo decision explicita de auditoria operativa (si/no y por que).
- [ ] Se tomo decision explicita de security event (si/no y por que).
- [ ] En denegaciones sensibles (permiso, MFA, SoD, anti-abuso), la respuesta es consistente y no filtra detalles sensibles.

### 5. Anti-abuso
- [ ] Se tomo decision explicita de politica operacional (rate limit/lockout/IP) para la accion.
- [ ] Si aplica endurecimiento, existe seed SQL/politica para `codigo_accion`.

### 6. Base de datos y enforcement
- [ ] Se reutiliza SP existente o se justifica SP nuevo.
- [ ] No hay SQL inline inseguro en API/Application.
- [ ] Persistencia usa ADO.NET tipado (sin `AddWithValue`).
- [ ] RLS oficial queda validado para el acceso de datos del endpoint.
- [ ] Si el endpoint escribe datos multiempresa, valida coherencia tenant/empresa/sesion.

### 7. Arquitectura por capa
- [ ] API contiene solo transporte HTTP (controller delgado).
- [ ] Caso de uso/handler en Application.
- [ ] Domain sin dependencias de ASP.NET ni SQL client.
- [ ] Infrastructure concentra persistencia y mapping DB.
- [ ] Contracts HTTP (`*RequestContract` / `*ResponseContract`) solo en `SecureERP.Api.Modules.*`.

### 8. Tests minimos obligatorios
- [ ] Test de autorizacion (permite con permiso correcto).
- [ ] Test de denegacion por permiso.
- [ ] Test de denegacion por MFA (si aplica).
- [ ] Test de aislamiento por RLS/contexto (tenant/empresa).
- [ ] Test de anti-abuso (si aplica).
- [ ] Test de auditoria/security event (si aplica).

### 9. Documentacion y catalogos
- [ ] Si hubo permiso nuevo: actualizado seed SQL + `Permissions` + `docs/security/permissions-catalog.md`.
- [ ] Si hubo politica operacional nueva: actualizado seed/politica y documentado en PR.
- [ ] Si hubo endpoint nuevo: documentado en modulo correspondiente (ej. `docs/modules/...`).

## B. DoD obligatorio para modulo nuevo

### 1. Contrato de seguridad del modulo
- [ ] Cada endpoint del modulo tiene permiso oficial.
- [ ] Cada endpoint tiene decision explicita de MFA.
- [ ] Cada endpoint tiene decision explicita de auditoria/security event.
- [ ] Cada endpoint tiene decision explicita de anti-abuso.

### 2. Datos y RLS
- [ ] Entidades multiempresa del modulo tienen estrategia RLS validada.
- [ ] SPs criticos del modulo aplican `SESSION_CONTEXT` y validaciones minimas.
- [ ] No se crean rutas alternas de seguridad fuera del motor oficial.

### 3. Calidad de entrega
- [ ] Suite de tests del modulo pasa en `main`.
- [ ] Guardrails de arquitectura/seguridad pasan.
- [ ] Documentacion del modulo queda como referencia para futuros endpoints.

## Referencias normativas internas
- `docs/security/layer-responsibility-contract.md`
- `docs/security/secure-endpoint-standard.md`
- `docs/security/permissions-catalog.md`
- `tests/SecureERP.Tests/Architecture/SecurityGuardrailEnforcementTests.cs`
