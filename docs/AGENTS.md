 # SecureERP — Protocolo Maestro para Codex

## PROPÓSITO
Guiar a Codex para trabajar sobre SecureERP con continuidad total del baseline existente, sin rediseños, sin duplicaciones y sin romper el modelo enterprise oficial.

Autor: Mario R. Gomez

---

## REGLAS CRÍTICAS (NO NEGOCIABLES)

1. NO crear nuevos Stored Procedures si ya existe uno equivalente.
2. SIEMPRE reutilizar los Stored Procedures existentes.
3. La base de datos es la fuente oficial de verdad.
4. El sistema ya posee un modelo enterprise — NO rediseñarlo.
5. NO generar SQL inline en API ni en Application.
6. NO omitir ni bypass funciones de seguridad ni Row Level Security (RLS).
7. NO crear lógica alternativa de acceso a datos.
8. NO duplicar lógica ya implementada en base de datos.
9. NO sobrescribir objetos SQL existentes sin análisis previo y justificación explícita.
10. NO generar código nuevo antes de terminar el inventario técnico y el gap analysis.

---

## REGLAS PARA STORED PROCEDURES

Antes de crear cualquier Stored Procedure, Codex DEBE:

1. Buscar SP existentes en el proyecto de base de datos.
2. Revisar patrones de nombres (`usp_*`).
3. Evaluar reutilización o extensión del SP existente.
4. Verificar si el SP ya cubre total o parcialmente el caso de uso.
5. Clasificar el SP encontrado según:
   - reutilizable
   - reutilizable con endurecimiento
   - obsoleto/no usar
   - duplicado potencial
   - faltante

Solo se permite crear un nuevo SP si:
- NO existe uno equivalente, y
- Es requerido por una nueva capacidad funcional real, y
- El gap analysis ya fue entregado y aprobado.

Todo Stored Procedure debe cumplir:
- `SET NOCOUNT ON`
- `SET XACT_ABORT ON`
- `TRY/CATCH`
- transacción cuando aplique
- uso de `SESSION_CONTEXT`
- validación tenant / empresa / usuario / sesión
- auditoría obligatoria
- respeto del alcance de datos
- sin bypass de funciones oficiales de seguridad
- sin SQL dinámico inseguro

---

## BASE DE DATOS COMO FUENTE DE VERDAD

La lógica de seguridad reside en la base de datos.

Los permisos se resuelven mediante:
- `seguridad.fn_usuario_permisos_efectivos`

El alcance de datos se resuelve mediante:
- `seguridad.fn_rls_tenant_empresa`

NO implementar lógica duplicada en otras capas.

NO crear policy RLS paralela.

NO reemplazar funciones oficiales por lógica en C#, API, Application o UI.

---

## REGLAS PARA API

- La API DEBE consumir Stored Procedures en operaciones críticas.
- La API NO debe contener lógica de negocio ya implementada en base de datos.
- La API DEBE validar:
  - sesión
  - permisos
  - contexto tenant / empresa
  - reglas del proceso si aplica
- La API NO debe reinterpretar seguridad que ya está resuelta por BD.

---

## REGLAS PARA UI

- La UI NUNCA implementa seguridad.
- La UI SOLO consume permisos ya resueltos.
- Ocultar botones NO sustituye validación backend.
- La visibilidad de acciones es una ayuda de experiencia de usuario, no un control de seguridad.

---

## FLUJO OBLIGATORIO DE TRABAJO

Codex DEBE trabajar en estas fases y en este orden:

### Fase 1 — Inventario técnico
Entregar inventario de:
- esquemas
- tablas por esquema
- funciones por esquema
- vistas por esquema
- stored procedures por esquema
- objetos críticos de seguridad
- objetos de RLS
- objetos de workflow
- objetos de auditoría
- dependencias relevantes
- riesgos detectados

### Fase 2 — Gap analysis
Entregar análisis de brecha indicando:
- qué ya existe
- qué es reutilizable
- qué requiere endurecimiento
- qué falta realmente
- qué no debe tocarse
- qué riesgos existen
- prioridad sugerida por módulo

### Fase 3 — Plan de implementación
Entregar plan por módulos:
- Security
- Organization
- Workflow
- Documents
- Activity
- Logistics

Cada plan debe indicar:
- orden recomendado
- SP a reutilizar
- SP faltantes
- endpoints asociados
- permisos asociados
- riesgos

### Fase 4 — Generación controlada
Solo después del análisis previo, generar código por lote pequeño:
- un módulo a la vez
- un submódulo a la vez
- sin mezclar varios dominios en una sola entrega

---

## FORMATO DE SALIDA OBLIGATORIO

Antes de proponer cambios, Codex debe responder siempre con:

1. Baseline entendido en 10 puntos
2. Clasificación:
   - YA RESUELTO
   - PENDIENTE REAL
   - MEJORA EVOLUTIVA
   - NO RECOMENDADO
3. Inventario o gap analysis según la fase
4. Propuesta concreta siguiente
5. Código solo si fue solicitado explícitamente en esa fase

---

## EN CASO DE DUDA

- CONSULTAR antes de crear nuevos Stored Procedures.
- NO asumir equivalencias.
- NO improvisar.
- NO generar artefactos masivos sin control.

---

## PRINCIPIO GENERAL

Codex debe actuar como arquitecto enterprise y ejecutor disciplinado, no como generador automático de código.

Debe priorizar:
- consistencia
- reutilización
- seguridad
- trazabilidad
- cumplimiento del baseline
- implementación por delta controlado
