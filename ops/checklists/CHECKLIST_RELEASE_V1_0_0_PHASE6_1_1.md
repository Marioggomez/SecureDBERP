# Checklist Release v1.0.0 Phase 6.1.1

## Pre-release

1. Confirmar backup full + log de la base objetivo.
2. Confirmar conexion a BD correcta (no ejecutar sobre master).
3. Confirmar permisos para ALTER PROCEDURE y lectura de catalogos de seguridad.

## Ejecucion

1. Ejecutar `sql/99_release/v1.0.0_phase6_1_1.sql` con sqlcmd.
2. Validar salida sin errores THROW no esperados.

## Post-release

1. Validar RLS:
- policy `RLS_scope_tenant_empresa` habilitada.
- cobertura multiempresa completa.
2. Validar endurecimiento:
- `plataforma.usp_exec_sql_if_needed` bloquea SQL dinamico.
- `dbo.usp_sysdiagrams_*` devuelve errores 52901-52905.
3. Registrar evidencia de fecha/hora y responsable.
