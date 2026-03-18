# SecureERP PR Checklist Oficial - Endpoints y Modulos

## Instruccion
Este checklist es obligatorio para PRs que agreguen o modifiquen endpoints, SPs, permisos o reglas de seguridad.

## 1) Alcance del cambio
- [ ] Indique modulo/feature afectado.
- [ ] Indique endpoints agregados/modificados.
- [ ] Indique SPs agregados/modificados.

## 2) Permisos y catalogo
- [ ] Que permiso usa cada endpoint? (listar `MODULO.ENTIDAD.ACCION` por ruta)
- [ ] El permiso es nuevo o existente?
- [ ] Si es nuevo: se agrego en seed SQL oficial?
- [ ] Si es nuevo: se agrego en `Permissions`?
- [ ] Se verifico naming oficial `MODULO.ENTIDAD.ACCION`?
- [ ] Se confirmo unicidad de constantes de permisos?

## 3) Endpoint security contract
- [ ] Todos los endpoints no publicos usan `[RequirePermission(...)]`.
- [ ] Se declaro `requiresMfa` explicitamente cuando corresponde.
- [ ] La decision de MFA para cada endpoint quedo documentada.
- [ ] La respuesta de denegacion es uniforme y segura (sin fuga de detalle sensible).

## 4) Auditoria y security events
- [ ] Este cambio requiere auditoria operativa? (si/no + motivo)
- [ ] Este cambio requiere security event? (si/no + motivo)
- [ ] Se cubrieron denegaciones por permiso/MFA/SoD/anti-abuso cuando aplica?

## 5) RLS y contexto
- [ ] Que politica RLS aplica a las tablas impactadas?
- [ ] Se valido aislamiento por tenant/empresa en lectura?
- [ ] Se valido coherencia tenant/empresa/sesion en escritura?
- [ ] Se evitaron filtros paralelos en frontend como sustituto de RLS?

## 6) Anti-abuso
- [ ] Que perfil anti-abuso aplica a cada endpoint sensible?
- [ ] Si hubo politica nueva: quedo en seed SQL/politica operacional?
- [ ] Se agregaron pruebas de rate-limit/lockout/IP policy cuando aplica?

## 7) Capas y arquitectura
- [ ] API contiene solo transporte HTTP (sin logica de negocio).
- [ ] Application contiene casos de uso/orquestacion.
- [ ] Domain no usa ASP.NET ni SQL client.
- [ ] Infrastructure concentra ADO.NET.
- [ ] Contracts HTTP quedaron solo en `SecureERP.Api.Modules.*`.
- [ ] Se respeta `docs/security/layer-responsibility-contract.md`.

## 8) SQL y persistencia
- [ ] Se reutilizo SP existente o se justifico uno nuevo.
- [ ] SQL critico via SPs o scripts controlados.
- [ ] Sin `AddWithValue`.
- [ ] Sin SQL inline inseguro.
- [ ] Scripts idempotentes.

## 9) Pruebas obligatorias del PR
- [ ] Autorizacion: caso permitido.
- [ ] Autorizacion: caso denegado por permiso.
- [ ] MFA: denegacion cuando aplica.
- [ ] RLS: no visibilidad interempresa.
- [ ] Auditoria/security event: persistencia cuando aplica.
- [ ] Guardrails de arquitectura/seguridad en verde.

## 10) Documentacion y release
- [ ] Actualizado doc del modulo afectado.
- [ ] Actualizado catalogo de permisos (doc + seed + runtime) si aplica.
- [ ] Adjuntado plan de despliegue SQL (si hay cambios en DB).

## Evidencia minima requerida en descripcion del PR
- [ ] Lista de endpoints y permisos.
- [ ] Lista de SPs/tables/policies tocados.
- [ ] Resultado de tests ejecutados (comando + resultado).
- [ ] Decisiones explicitas: MFA, auditoria, security event, anti-abuso.
