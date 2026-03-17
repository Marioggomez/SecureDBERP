\# SecureERP — Stored Procedure Standards SQL Server



\## Propósito

Definir el estándar oficial para Stored Procedures.



\## Convenciones de nombre

\- usp\_xxx\_crear

\- usp\_xxx\_actualizar

\- usp\_xxx\_obtener

\- usp\_xxx\_listar

\- usp\_xxx\_activar

\- usp\_xxx\_desactivar



\## Estructura base

\- SET NOCOUNT ON

\- SET XACT\_ABORT ON

\- lectura de SESSION\_CONTEXT

\- validación de contexto

\- TRY/CATCH

\- transacción si aplica

\- auditoría

\- retorno consistente



\## Contexto mínimo esperado

\- id\_tenant

\- id\_empresa

\- id\_usuario

\- id\_sesion



\## Validaciones obligatorias

\- contexto válido

\- tenant/empresa consistentes

\- alcance permitido

\- estado del registro

\- seguridad de operación



\## Reglas técnicas

\- evitar SQL dinámico inseguro

\- evitar lógica duplicada

\- evitar SP monstruo multipropósito

\- paginación real en listados

\- filtros opcionales bien controlados

\- índices alineados con consultas reales



\## Manejo de errores

\- THROW con mensajes controlados

\- no ocultar errores críticos

\- dejar trazabilidad técnica



\## Auditoría

Toda operación crítica debe dejar evidencia funcional y técnica según corresponda.



\---

Autor: Mario R. Gomez

Proyecto: SecureERP

Estado: Baseline

\---

