# adr-0001-solution-structure

## título
ADR-0001 - Estructura Inicial de Solución SecureERP

## propósito
Registrar la decisión arquitectónica de arranque para asegurar consistencia técnica en las siguientes fases del proyecto.

## alcance
Cubre la estructura de proyectos, relaciones entre capas y lineamientos de acceso a datos en la solución inicial.

## contexto
SecureERP requiere evolucionar de forma incremental sobre una base de datos ya endurecida, evitando rediseños radicales y sin crear microservicios prematuros.

## decisiones
- Se crea `SecureERP.sln` con seis proyectos: Api, Application, Domain, Infrastructure, Database y Tests.
- Los módulos funcionales viven como carpetas por feature dentro de cada capa.
- Se define ADO.NET puro para SQL Server con ejecución de Stored Procedures y parámetros tipados.
- Se centraliza middleware técnico en Api (correlationId + exception handling).
- Se mantiene la seguridad crítica en base de datos (RLS + SESSION_CONTEXT).

## riesgos
- Implementaciones ad hoc fuera del patrón de SP tipado pueden romper controles de seguridad.
- Falta de disciplina de módulo puede derivar en crecimiento horizontal desordenado.

## dependencias
- Baseline de seguridad y estándares SQL ya definidos en documentación previa.
- Repositorio SQL existente (`/sql`) y su estrategia de releases.

## pendientes
- Definir ADRs siguientes para autenticación real, auditoría operacional y estrategia de testing E2E.
- Consolidar pipeline de validación de arquitectura y quality gates.

## autor
Codex (arquitectura inicial bajo lineamientos de Mario R. Gomez)

## estado
Aceptado
