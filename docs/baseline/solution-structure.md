# solution-structure

## título
Estructura de Solución Inicial SecureERP

## propósito
Definir una base estable, gobernada y mantenible para evolucionar SecureERP por fases sin romper baseline de seguridad ni contratos existentes de base de datos.

## alcance
Incluye la solución .NET, proyectos base, carpetas por feature y convenciones mínimas de arranque para API, Application, Domain, Infrastructure, Database y Tests.

## contexto
El sistema ya dispone de base de datos endurecida con RLS, SESSION_CONTEXT y Stored Procedures reutilizables. Esta fase crea la solución inicial sin rediseñar la BD ni introducir sobrearquitectura.

## decisiones
- Se adopta modular monolith con carpetas por módulo funcional.
- Se usa Clean Architecture simplificada con 4 capas de código más proyecto Database.
- El acceso a datos se prepara con ADO.NET y ejecución controlada de SP.
- No se utiliza Entity Framework.
- La seguridad de datos permanece en SQL Server como fuente de verdad.

## riesgos
- Si se omite la propagación consistente de contexto de sesión, puede degradarse el aislamiento esperado por RLS.
- Si se agregan consultas ad hoc fuera del executor tipado, aumenta riesgo de SQL inseguro.

## dependencias
- SQL Server con objetos endurecidos existentes.
- .NET 8 SDK.
- Convenciones documentales y de seguridad del baseline SecureERP.

## pendientes
- Implementar casos de uso por módulo en lotes incrementales.
- Conectar handlers con SP específicos clasificados como reutilizables.
- Completar pruebas de integración contra ambiente SQL controlado.

## autor
Codex (arquitectura inicial bajo lineamientos de Mario R. Gomez)

## estado
Aprobado para arranque técnico
