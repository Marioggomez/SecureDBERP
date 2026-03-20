# SecureERP Desktop

Base inicial del cliente ERP de escritorio sobre .NET 8 + WinForms + DevExpress 2025.

## Estructura

- `SecureERP.Desktop.Shell`: shell principal (Ribbon + Accordion + DocumentManager/TabbedView + DockManager)
- `SecureERP.Desktop.Modules`: modulos UI y plantillas reutilizables de pantallas
- `SecureERP.Desktop.Core`: contratos y abstracciones del cliente desktop
- `SecureERP.Desktop.Infrastructure.SecureApi`: integracion HTTP con APIs de SecureERP (auth, permisos, catalogos)

## Variables de entorno

- `SECUREERP_API_BASEURL`: URL base del backend SecureERP para login y catalogos.

## Notas

- WinForms corre solo en Windows.
- Esta base no modifica backend ni IAM core.
- El menu se arma desde modulos y permisos (`PermissionKey` por item).