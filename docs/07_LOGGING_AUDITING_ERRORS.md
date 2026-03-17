\# SecureERP — Logging, Auditing and Error Handling



\## Propósito

Definir la estrategia oficial de logging técnico, auditoría funcional y manejo de errores.



\## Capas diferenciadas

1\. Logging técnico

2\. Auditoría funcional

3\. Eventos de seguridad



\## Logging técnico

Debe capturar:

\- timestamp UTC

\- nivel

\- correlationId

\- endpoint

\- modulo

\- usuario

\- tenant

\- empresa

\- sesión

\- excepción

\- stack resumido

\- payload resumido seguro

\- ip

\- user-agent



\## Auditoría funcional

Debe registrar acciones de negocio y administración:

\- login/logout

\- cambio de empresa

\- crear/editar

\- activar/desactivar

\- aprobar/rechazar

\- exportar/imprimir

\- comentarios

\- etiquetas

\- documentos

\- asignaciones de seguridad



\## Eventos de seguridad

\- login fallido

\- acceso denegado

\- MFA fallida

\- refresh token reuse

\- sesión expirada

\- sesión revocada



\## Manejo de errores

Todo error debe producir:

\- mensaje humano para usuario

\- mensaje técnico para desarrollador

\- correlationId

\- código de error



\## Reglas

\- nunca mostrar stack trace al usuario

\- siempre devolver correlationId

\- exception middleware obligatorio

\- health checks obligatorios



\---

Autor: Mario R. Gomez

Proyecto: SecureERP

Estado: Baseline

\---

