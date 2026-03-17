# SecureERP - Fase 4 Refactor Excepciones

Fecha: 2026-03-17
Estado: completado

## Objetivo

Resolver las 6 excepciones pendientes:
- 5 SP dbo.usp_sysdiagrams_*
- 1 SP plataforma.usp_exec_sql_if_needed

## Cambios aplicados

### 1) Aislamiento sysdiagrams

SP afectados:
1. [dbo].[usp_sysdiagrams_actualizar]
2. [dbo].[usp_sysdiagrams_crear]
3. [dbo].[usp_sysdiagrams_desactivar]
4. [dbo].[usp_sysdiagrams_listar]
5. [dbo].[usp_sysdiagrams_obtener]

Resultado:
- Se retiro su uso operativo.
- Quedaron con THROW controlado (errores 52901-52905).
- Se eliminaron dependencias rotas sobre dbo.sysdiagrams.

### 2) Bloqueo SQL dinamico generico

SP afectado:
- [plataforma].[usp_exec_sql_if_needed]

Resultado:
- Se elimino ejecucion dinamica generica insegura.
- Si @sql viene vacio: retorna "skipped-empty".
- Si @sql trae contenido: THROW 52906.

## Respaldo

- Tabla: plataforma.sp_hardening_backup
- run_id de este lote: 28743D05-02E8-4E4D-B630-EF483F132B05
- Objetos respaldados: 6

## Validacion tecnica

- Dependencias locales no resueltas: 0
- SP con patron dinamico bloqueado (sp_executesql @sql): 0
- Clasificacion global:
  - reutilizable: 547
  - reutilizable con endurecimiento: 0
  - no reutilizable hasta refactor: 0

## Smoke checks ejecutados

- dbo.usp_sysdiagrams_listar -> PASS (error 52901 esperado)
- plataforma.usp_exec_sql_if_needed @sql='SELECT 1' -> PASS (error 52906 esperado)
- plataforma.usp_exec_sql_if_needed @sql='' -> PASS (ejecutado=0, estado='skipped-empty')

## Impacto funcional

- sysdiagrams queda oficialmente fuera de operacion en este entorno.
- Cualquier invocacion al ejecutor dinamico generico queda bloqueada por seguridad.
- No se crearon nuevos Stored Procedures.
