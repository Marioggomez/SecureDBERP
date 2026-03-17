# SecureDBERP

Repositorio oficial de la solución de base de datos para SecureERP.

## Estado actual

- Fase 1 a Fase 5 completadas y certificadas.
- Endurecimiento SQL aplicado sobre todos los Stored Procedures.
- Clasificación final: 547 SP reutilizables.

## Estructura

- `docs/`: baseline, decisiones y actas de certificación.
- `sql/00_prerequisites/`: prerrequisitos de ambiente.
- `sql/01_schemas/`: creación y mantenimiento de esquemas.
- `sql/02_tables/`: definición de tablas.
- `sql/03_functions/`: funciones escalares/tabla.
- `sql/04_views/`: vistas de negocio y soporte.
- `sql/05_procedures/`: procedimientos almacenados.
- `sql/06_security_rls/`: seguridad, permisos, RLS.
- `sql/07_auditoria_observabilidad/`: auditoría y trazabilidad.
- `sql/08_workflow/`: objetos de workflow/aprobación.
- `sql/09_seed/`: datos semilla controlados.
- `sql/99_release/`: paquetes de despliegue por versión.
- `ops/`: runbooks y checklists de operación.

## Convenciones iniciales

1. No modificar contratos funcionales sin declarar impacto.
2. Todo cambio SQL debe mantener XACT_ABORT + TRY/CATCH.
3. Escritura multiempresa requiere SESSION_CONTEXT y validación de tenant/empresa.
4. Cambios de seguridad/auditoría deben incluir evidencia de pruebas.

## Actas de referencia

- `docs/FASE_3_3_CIERRE_OPERATIVO.md`
- `docs/FASE_4_REFACTOR_EXCEPCIONES.md`
- `docs/FASE_5_CERTIFICACION_FINAL.md`
