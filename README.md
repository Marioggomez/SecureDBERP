# SecureERP

Solution base enterprise para SecureERP sobre .NET 8 + SQL Server (ADO.NET + Stored Procedures).

## Proyectos

- `src/SecureERP.Api`
- `src/SecureERP.Application`
- `src/SecureERP.Domain`
- `src/SecureERP.Infrastructure`
- `src/SecureERP.Database`
- `tests/SecureERP.Tests`

## Principios

- Modular monolith gobernado por features (no microservicios)
- Base de datos como fuente de verdad en seguridad
- Reutilización de Stored Procedures existentes
- Sin Entity Framework
- Sin SQL dinámico inseguro
- Sin `AddWithValue`

## Referencias entre proyectos

- Api -> Application, Infrastructure
- Application -> Domain
- Infrastructure -> Application, Domain
- Domain -> (sin referencias a otros proyectos)
- Database -> sin referencias desde C#

## Estado

Estructura inicial creada y lista para iteración incremental por fases.

## Configuracion Segura (obligatoria)

- No se permiten credenciales reales en archivos versionados.
- Para API local, usar:
  - `ConnectionStrings__SecureERP` (variable de entorno), o
  - `dotnet user-secrets` en `src/SecureERP.Api`.
- Para tests de integracion:
  - `SECUREERP_SQL_CONNECTION_STRING` (variable de entorno), o
  - `database.config.json` local (ignorado por git).

Referencia oficial:
- `docs/security/secrets-policy.md`

## Certificacion de Main

- Runbook oficial: `ops/runbooks/RUNBOOK_CERTIFICACION_MAIN_P1.md`
- Plantilla de reporte: `ops/reports/CERTIFICACION_MAIN_TEMPLATE.md`

