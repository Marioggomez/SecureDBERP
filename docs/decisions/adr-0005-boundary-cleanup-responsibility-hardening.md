# ADR-0005 Boundary Cleanup And Responsibility Hardening

## Titulo
Correccion pragmatica de limites entre capas para continuidad de desarrollo

## Proposito
Eliminar mezcla real de responsabilidades detectada y formalizar reglas de capa para evitar contaminacion futura.

## Alcance
- `SecureERP.Api`
- `SecureERP.Application`
- `SecureERP.Domain`
- `SecureERP.Infrastructure`
- metadata de seguridad (codigo/DB/documentacion)

## Hallazgo principal corregido
- `SqlServerReadyHealthCheck` estaba en `Api` usando `Microsoft.Data.SqlClient`.
- Se movio a `Infrastructure` (`SecureERP.Infrastructure.Health.SqlServerReadyHealthCheck`).
- `Api` conserva solo transporte/registro HTTP de health endpoints.

## Decisiones
1. `Api` no implementa ADO.NET ni acceso SQL directo.
2. `Infrastructure` concentra implementaciones SQL (`SqlConnection`, `SqlCommand`, `SqlParameter`).
3. Se agregan tests de arquitectura para proteger el boundary:
   - Api sin `SqlClient`
   - Application sin tipos ASP.NET
   - Domain sin ASP.NET ni SQL client

## Regla oficial DTO por capa
- Api: contratos HTTP (request/response contracts).
- Application: DTOs de caso de uso (commands/queries/results).
- Domain: tipos puros de dominio.
- Infrastructure: snapshots/mapping de persistencia, no contratos HTTP.

## Jerarquia oficial de metadata de seguridad
1. Fuente operativa de verdad: Base de datos (`seguridad.permiso`, `seguridad.politica_operacion_api`, RLS/SP/politicas).
2. Fuente de verdad de codigo: `Permissions` + `RequirePermission`.
3. Fuente documental: `docs/security/*` y ADRs.
4. Consumo en runtime: middleware + authorization evaluator + handlers.

## Riesgos
- Ninguno funcional esperado. Movimiento compatible sin cambios de contrato API.

## Dependencias
- `src/SecureERP.Api/Extensions/ServiceCollectionExtensions.cs`
- `src/SecureERP.Infrastructure/Health/SqlServerReadyHealthCheck.cs`
- `tests/SecureERP.Tests/Architecture/BoundaryResponsibilityRulesTests.cs`

## Estado
Aprobado
