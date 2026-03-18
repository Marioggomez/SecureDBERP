# ADR-0004 Error And Observability Contract

## Titulo
Contrato enterprise de errores y observabilidad para API

## Proposito
Unificar respuesta de error, correlacion y logging estructurado sin redisenar el IAM ni la Fase 5A.

## Alcance
- `CorrelationIdMiddleware`
- `ExceptionHandlingMiddleware`
- `SecurityContextMiddleware`
- endpoints `/health`, `/health/ready`, `/health/live`

## Contexto
- SecureERP requiere respuestas seguras, trazables y consistentes.
- Se evita exponer detalle tecnico en payload de error.
- El detalle tecnico se mantiene solo en logs estructurados.

## Decisiones
1. Contrato de error unificado (`ApiErrorResponse`):
   - `errorCode`
   - `message`
   - `correlationId`
2. Correlation ID:
   - se toma de `X-Correlation-Id` si viene en request
   - si no existe, se genera uno nuevo
   - se propaga a `TraceIdentifier`, `RequestContext` y header de response
3. Logging estructurado centralizado:
   - campos comunes: `correlationId`, `userId`, `tenantId`, `endpoint`, `errorCode`
   - niveles: `Information`, `Warning`, `Error`
4. Manejo global de excepciones:
   - `DomainException` => `400` con mensaje seguro
   - excepción no controlada => `500` con mensaje seguro
   - detalle tecnico solo en logs
5. Politica de sesion expirada/invalida:
   - respuesta uniforme `401`
   - `errorCode` permitido: `SESSION_EXPIRED` o `SESSION_INVALID`
6. Health endpoints:
   - `/health`: estado general
   - `/health/ready`: readiness incluyendo SQL Server
   - `/health/live`: liveness simple

## Riesgos
- El contrato uniforme reduce detalle de error para cliente final.
- Errores funcionales legacy fuera de middleware aun pueden conservar contratos previos por compatibilidad.

## Dependencias
- `IRequestContextAccessor`
- `SecureERP` connection string
- `Microsoft.Extensions.Diagnostics.HealthChecks`
- `Microsoft.Data.SqlClient`

## Pendientes
- Migrar endpoints legacy que hoy devuelven contratos propios de error al contrato comun en una fase controlada.
- Versionar el contrato si se decide introducir `429 + Retry-After` en todos los paths de rate limiting.

## Autor
Codex (SecureERP)

## Estado
Aprobado para Fase 5B
