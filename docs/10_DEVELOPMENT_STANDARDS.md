\# SecureERP — Development Standards



\## Propósito

Definir estándares técnicos de desarrollo.



\## Reglas generales

\- código limpio

\- nombres claros

\- separación por feature

\- async en backend

\- validación explícita

\- no lógica de negocio en controller

\- no acceso SQL directo desde UI o API



\## Backend

\- handlers por caso de uso

\- DTOs claros

\- validadores

\- middleware centralizado

\- contratos bien definidos



\## Base de datos

\- SPs con estándar oficial

\- sin SQL inseguro

\- con auditoría y seguridad

\- paginación real



\## UI

\- permisos dinámicos

\- componentes reutilizables

\- no hardcodear seguridad



\## Testing

\- pruebas de dominio

\- pruebas de integración

\- pruebas de seguridad

\- pruebas de arquitectura cuando aplique



\## Definition of Done

Una tarea no está terminada si no incluye:

\- código

\- validación

\- logging/auditoría si aplica

\- documentación

\- revisión técnica



\---

Autor: Mario R. Gomez

Proyecto: SecureERP

Estado: Baseline

\---

