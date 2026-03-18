# ADR-0003 Fase 5A.1 Operational Consistency

## Titulo
Correccion minima de consistencia operacional para anti-abuso IAM

## Proposito
Cerrar ajustes puntuales de Fase 5A sin rediseño: principal key en `AUTH.VALIDATE_SESSION`, precedencia operacional de login, alcance real de `politica_ip` y decision de contrato HTTP para rate limiting.

## Alcance
Aplica a login, validate-session y evaluacion de politicas IP del IAM Core actual.

## Contexto
- Fase 5A ya implemento rate limit, lockout e IP policy.
- `AUTH.VALIDATE_SESSION` estaba limitando por IP sin principal estable adicional.
- El SP `seguridad.usp_security_ip_policy_evaluar` compara `p.ip_o_cidr = @ip_origen` (match exacto).

## Decisiones
1. `AUTH.VALIDATE_SESSION` usa doble dimension de rate limiting:
   - IP (`scope=IP`)
   - principal derivada de hash de token (`scope=PRINCIPAL`, prefijo `SESSION:` + 16 bytes iniciales de SHA-256 en hex)
2. Precedencia operacional de login formal:
   1) IP policy  
   2) rate limit  
   3) lockout  
   4) validacion de credenciales
3. `politica_ip` se declara oficialmente como `exact match` en esta fase.
   - CIDR no se declara soportado hasta implementar parser/matcher real.
4. HTTP `429 + Retry-After`:
   - No se cambia en 5A.1 para no romper el contrato uniforme actual (`AUTH_REQUEST_REJECTED`).
   - Queda pendiente controlado para siguiente ajuste del contrato de errores/observabilidad.

## Riesgos
- Mantener respuesta uniforme evita enumeracion, pero reduce expresividad HTTP para clientes.
- Sin soporte CIDR real, operaciones con rangos deben registrarse como pendientes operativos.

## Dependencias
- `SecureERP.Application.Modules.Security.Queries.ValidateSessionHandler`
- `seguridad.usp_security_ip_policy_evaluar`
- `seguridad.usp_security_rate_limit_evaluar`

## Pendientes
- Definir versionado de contrato para habilitar `429 + Retry-After` sin romper clientes existentes.
- Implementar CIDR real en `politica_ip` cuando se apruebe alcance (IPv4/IPv6 + pruebas de regresion).

## Autor
Codex (SecureERP IAM)

## Estado
Aprobado para 5A.1 (correccion minima)
