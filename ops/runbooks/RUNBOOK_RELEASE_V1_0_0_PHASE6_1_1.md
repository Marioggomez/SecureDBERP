# Runbook Release v1.0.0 Phase 6.1.1

## Comando sugerido (sqlcmd)

```powershell
sqlcmd -S <server> -d <database> -U <user> -P <password> -i "sql/99_release/v1.0.0_phase6_1_1.sql"
```

## Validaciones recomendadas despues de ejecutar

```sql
SELECT is_enabled FROM sys.security_policies WHERE name = N'RLS_scope_tenant_empresa';
```

```sql
EXEC plataforma.usp_exec_sql_if_needed @sql = N'SELECT 1'; -- debe lanzar 52906
```

```sql
EXEC dbo.usp_sysdiagrams_listar; -- debe lanzar 52901
```

## Criterio de exito

- Script release ejecuta completo.
- No hay dependencias locales sin resolver.
- RLS permanece habilitada y completa.
- Endurecimiento de excepciones Fase 4 permanece efectivo.
