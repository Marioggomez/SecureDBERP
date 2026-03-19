# SecureERP Secrets Policy (Prioridad 0)

## Alcance
Aplica a todo archivo versionado en el repositorio.

## Prohibicion
No se permite versionar:
- connection strings con `User Id` + `Password`
- tokens de acceso
- API keys
- client secrets
- credenciales de infraestructura

## Convencion obligatoria de configuracion

### API local
1. `appsettings.json` y `appsettings.Development.json` deben contener valores seguros sin secretos.
2. La conexion real debe venir por:
   - variable de entorno `ConnectionStrings__SecureERP`, o
   - secret manager local (`dotnet user-secrets`) en el proyecto API.

### Tests de integracion local
1. `database.config.example.json` es solo referencia segura (sin credenciales reales).
2. La conexion real debe ir en:
   - variable de entorno `SECUREERP_SQL_CONNECTION_STRING`, o
   - archivo local ignorado `database.config.json`.

### CI/CD
1. El valor real de SQL debe inyectarse como secreto del repositorio:
   - `SECUREERP_SQL_CONNECTION_STRING`
2. No usar valores hardcoded en workflows.

## Rotacion obligatoria inmediata
Al detectar credencial en repo:
1. Rotar inmediatamente password/secret comprometido.
2. Invalidar sesiones/tokens dependientes.
3. Actualizar secreto en gestor seguro (GitHub Secrets o equivalente).
4. Verificar que el valor removido ya no exista en historia activa de la rama.

## Enforcement minimo
1. Secret scan obligatorio en CI: `ops/security/scan-secrets.ps1`
2. PR debe fallar si se detecta secreto expuesto.
3. No se aprueba PR con excepciones manuales a esta politica sin aprobacion de arquitectura y seguridad.
