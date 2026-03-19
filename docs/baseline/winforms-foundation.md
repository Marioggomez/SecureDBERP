# Baseline WinForms SecureERP

## Propósito
Establecer un framework UI enterprise para cliente desktop WinForms + DevExpress 2025 sobre .NET 8, reutilizable para módulos ERP futuros.

## Alcance
Incluye shell MDI con Ribbon, navegación modular, splash screen, gestión de skins y plantilla base de búsquedas con filtros, paginado y loading visual.

## Estructura
- `Shell`: contenedor principal MDI + Ribbon + navegación.
- `Splash`: pantalla de arranque con estado de inicialización.
- `Themes`: persistencia local y aplicación de apariencia.
- `Search`: base reusable para listados y búsquedas paginadas.
- `Controls/RelatedInfo`: componentes comunes (documentos, notas, etiquetas).
- `Modules`: formularios concretos iniciales (`Home`, `System`, `Search`).
- `Services`: catálogo de navegación y proveedores de búsqueda.
- `Infrastructure/Ui`: utilidades visuales (busy overlay).

## Reglas de extensión
1. Nuevos módulos deben registrarse en `NavigationModuleCatalog`.
2. Nuevas búsquedas deben heredar de `SearchTemplateFormBase<T>`.
3. Componentes transversales deben vivir en `Controls`.
4. Persistencia de preferencias visuales debe pasar por `IThemePreferenceService`.

## Pendientes
- Conectar proveedores de búsqueda al backend SecureERP.Api real.
- Incorporar autorización por permiso en navegación y acciones.
- Integrar telemetría UI y políticas de sesión en cliente desktop.
