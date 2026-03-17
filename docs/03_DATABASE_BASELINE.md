\# SecureERP — Database Baseline



\## Propósito

Definir la estructura base y reglas de diseño de la base de datos SQL Server.



\## Motor

SQL Server



\## Reglas obligatorias

\- toda tabla operativa multiempresa debe considerar id\_tenant e id\_empresa

\- usar Stored Procedures endurecidos para operaciones críticas

\- usar integridad referencial estricta

\- usar índices útiles por operación real

\- no usar SQL inline inseguro

\- no modelar la BD como simple reflejo de pantalla



\## Esquemas principales

\- seguridad

\- organizacion

\- cumplimiento

\- workflow

\- documento

\- actividad

\- logistica

\- catalogo

\- core



\## Componentes ya asumidos

\- seguridad.fn\_rls\_tenant\_empresa

\- funciones canónicas de permisos y alcance

\- cumplimiento.auditoria\_operacion

\- workflow enterprise

\- documentos

\- comentarios

\- etiquetas



\## Reglas de integridad

\- FKs estrictas

\- constraints de unicidad donde apliquen

\- soft delete o estado preferido sobre hard delete

\- trazabilidad de creación/modificación cuando aplique



\## Convenciones de SP

\- usp\_xxx\_crear

\- usp\_xxx\_actualizar

\- usp\_xxx\_obtener

\- usp\_xxx\_listar

\- usp\_xxx\_activar

\- usp\_xxx\_desactivar



\## Reglas de ejecución de SP

\- SET NOCOUNT ON

\- SET XACT\_ABORT ON

\- TRY/CATCH

\- transacción cuando aplique

\- lectura de SESSION\_CONTEXT

\- validación de tenant/empresa/usuario/sesión

\- auditoría

\- sin bypass de alcance



\---

Autor: Mario R. Gomez

Proyecto: SecureERP

Estado: Baseline

\---

