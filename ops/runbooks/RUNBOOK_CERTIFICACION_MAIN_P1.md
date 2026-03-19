# Runbook Certificacion Reproducible de `main` (Prioridad 1)

## Objetivo
Ejecutar certificacion tecnica de `main` con evidencia reproducible.

## Toolchain obligatorio
- SDK fijado por `global.json` (net8).
- No usar SDK fuera de version fijada para certificar.

## Gates PR obligatorios
1. `ci-main`:
   - secret scan
   - restore
   - build
   - tests no integracion
   - guardrails arquitectura/seguridad
2. `integration-sql` (job separado con prerequisito de secreto SQL):
   - integration + E2E IAM

## Clasificacion oficial de tests
- Siempre en PR:
  - todo test que **no** sea `Integration`
  - guardrails de `Architecture` y `Security`
- SQL requerido (job separado):
  - tests `FullyQualifiedName~Integration`

## Gate SQL de integracion (readiness)
### Prerequisitos
1. Secreto `SECUREERP_SQL_CONNECTION_STRING` configurado.
2. Base con baseline SQL aplicado.
3. Conectividad de runner a SQL habilitada.

### Comando
```powershell
dotnet test tests/SecureERP.Tests/SecureERP.Tests.csproj --configuration Release --no-build --filter "FullyQualifiedName~Integration"
```

### Evidencia minima
- archivo TRX de resultados
- resumen pass/fail
- timestamp
- commit SHA

## Reporte unico de certificacion
Usar la plantilla:
- `ops/reports/CERTIFICACION_MAIN_TEMPLATE.md`
