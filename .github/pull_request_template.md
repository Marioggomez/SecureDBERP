## Tipo de cambio
- [ ] Nuevo endpoint
- [ ] Modificacion de endpoint existente
- [ ] Nuevo modulo
- [ ] Cambio SQL (tablas/SP/RLS/seed)
- [ ] Refactor sin cambio funcional

## Alcance
Describa modulo, endpoints y objetos DB impactados.

## Checklist obligatorio

### Permisos
- [ ] El endpoint usa permiso oficial `MODULO.ENTIDAD.ACCION`.
- [ ] Si el permiso es nuevo, se agrego a `Permissions`.
- [ ] Si el permiso es nuevo, se agrego a seed SQL oficial y documentacion.

### Seguridad por endpoint
- [ ] Todos los endpoints no publicos usan `[RequirePermission(...)]`.
- [ ] `requiresMfa` fue decidido explicitamente por endpoint.
- [ ] Auditoria fue decidida explicitamente por endpoint.
- [ ] Security event fue decidido explicitamente por endpoint.
- [ ] Anti-abuso fue decidido explicitamente por endpoint.

### RLS y contexto
- [ ] Se valido RLS para tenant/empresa en entidades afectadas.
- [ ] No se agregaron filtros paralelos que sustituyan RLS.
- [ ] Escrituras validan coherencia tenant/empresa/sesion.

### Arquitectura por capa
- [ ] API solo transporte HTTP.
- [ ] Application contiene casos de uso.
- [ ] Infrastructure contiene persistencia ADO.NET.
- [ ] Domain sin ASP.NET/SQL client.
- [ ] Contracts HTTP solo en `SecureERP.Api.Modules.*`.

### SQL y persistencia
- [ ] Sin `AddWithValue`.
- [ ] Sin SQL inline inseguro.
- [ ] SP reutilizado o justificado.
- [ ] Script SQL idempotente (si aplica).
- [ ] Sin secretos o credenciales reales en archivos versionados.

### Tests
- [ ] Pruebas de permiso (allow/deny).
- [ ] Pruebas de MFA (si aplica).
- [ ] Pruebas RLS (aislamiento interempresa).
- [ ] Pruebas de auditoria/security event (si aplica).
- [ ] Guardrails de arquitectura/seguridad en verde.

## Evidencia
Incluya aqui:
- Endpoints y permisos usados
- Comandos de test ejecutados y resultado
- Decisiones explicitas (MFA, auditoria, events, anti-abuso)
- Scripts SQL de release aplicados (si aplica)

## Referencias obligatorias
- `docs/checklists/endpoint-definition-of-done.md`
- `docs/checklists/pull-request-security-checklist.md`
- `docs/security/secure-endpoint-standard.md`
- `docs/security/layer-responsibility-contract.md`
