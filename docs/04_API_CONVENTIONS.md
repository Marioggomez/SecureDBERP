\# SecureERP — API Conventions



\## Propósito

Definir convenciones de rutas, versionado, tags Swagger, contratos y respuestas.



\## Versionado

\- versión inicial: v1

\- convención de ruta: /api/v1/{modulo}/{recurso}



\## Convención de rutas

\- usar minúsculas

\- usar plural para recursos

\- usar verbos solo en acciones especiales



\## Ejemplos

\- /api/v1/auth/login

\- /api/v1/security/users

\- /api/v1/workflow/instances/{id}/approve

\- /api/v1/logistics/quotes/{id}/submit-approval



\## Tags Swagger

\- Auth

\- Security.Users

\- Security.Roles

\- Security.Navigation

\- Security.Permissions

\- Organization.Companies

\- Organization.Branches

\- Organization.Units

\- Workflow.Approvals

\- Documents

\- Activity.Comments

\- Logistics.Quotes

\- Logistics.Shipments

\- Audit.Operations

\- System



\## Patrón mínimo por recurso

\- POST create

\- PUT update

\- GET by id

\- GET paged list/search

\- POST activate

\- POST deactivate



\## Respuestas estándar

\- 200

\- 201

\- 204

\- 400

\- 401

\- 403

\- 404

\- 409

\- 422

\- 500



\## Contratos

\- DTO de lista distinto al DTO de detalle

\- request/response explícitos

\- no devolver entidades de dominio directamente



\## Seguridad por endpoint

Cada endpoint debe documentar:

\- permiso requerido

\- si requiere MFA

\- si audita

\- si aplica RLS



\---

Autor: Mario R. Gomez

Proyecto: SecureERP

Estado: Baseline

\---

