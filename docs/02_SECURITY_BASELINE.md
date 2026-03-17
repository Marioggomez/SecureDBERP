\# SecureERP — Security Baseline



\## Propósito

Definir el baseline oficial de seguridad de SecureERP.



\## Principios de seguridad

\- Zero Trust

\- Least Privilege

\- Deny by default

\- defensa en profundidad

\- trazabilidad completa

\- segregación de funciones

\- autenticación fuerte

\- autorización por permisos y alcance



\## Modelo base

\- multi-tenant

\- multiempresa

\- usuarios

\- roles

\- permisos

\- excepciones ALLOW / DENY

\- scopes por empresa, unidad y usuario

\- ABAC complementando RBAC



\## Fuente de verdad oficial

Funciones canónicas:

\- seguridad.fn\_usuario\_empresas\_efectivas

\- seguridad.fn\_usuario\_unidades\_efectivas

\- seguridad.fn\_usuario\_usuarios\_efectivos

\- seguridad.fn\_usuario\_permisos\_efectivos



\## RLS oficial

\- SESSION\_CONTEXT como mecanismo base

\- función oficial: seguridad.fn\_rls\_tenant\_empresa

\- no crear policies paralelas



\## Tokens y sesiones

\- token opaco

\- refresh token opaco

\- hash persistido, no token en claro

\- expiración absoluta

\- expiración por inactividad

\- rotación de refresh token

\- revocación

\- detección de reuse

\- trazabilidad por usuario, tenant, empresa y sesión



\## MFA

Aplicar MFA al menos en:

\- login según política

\- acciones críticas

\- seguridad administrativa

\- aprobaciones sensibles

\- acceso a auditoría sensible



\## Validación obligatoria por request

\- sesión válida

\- token válido

\- tenant

\- empresa

\- usuario activo

\- permiso requerido

\- alcance de datos

\- regla de proceso

\- MFA si aplica



\## Eventos de seguridad obligatorios

\- login exitoso

\- login fallido

\- MFA emitida

\- MFA validada

\- MFA fallida

\- token refrescado

\- refresh reuse detectado

\- sesión revocada

\- acceso denegado

\- operación crítica bloqueada



\---

Autor: Mario R. Gomez

Proyecto: SecureERP

Estado: Baseline

\---

