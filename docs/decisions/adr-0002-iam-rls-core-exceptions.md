# ADR-0002 IAM Core RLS Exceptions

## Titulo
Excepciones controladas de RLS para tablas del nucleo IAM

## Proposito
Documentar por que `seguridad.sesion_usuario` y `seguridad.desafio_mfa` se excluyen del predicado tenant+empresa de la policy oficial RLS, y como se compensa el riesgo.

## Alcance
Aplica al flujo de autenticacion y sesion del IAM Core: `login -> MFA -> select-company -> session validation`.

## Contexto
- SecureERP usa RLS oficial de SQL Server con `SESSION_CONTEXT`.
- El flujo IAM de login y MFA opera en una etapa donde la empresa final puede no estar consolidada.
- Se requiere evitar bloqueos de autenticacion por filtros prematuros de tenant+empresa.

## Decisiones
- Mantener excepcion RLS para `seguridad.sesion_usuario`.
- Mantener excepcion RLS para `seguridad.desafio_mfa`.
- Reforzar autorizacion central con precedencia `DENY > ALLOW > effective > default deny`.
- Mantener trazabilidad via `seguridad.security_event_audit` y `observabilidad.auditoria_autorizacion`.

## Riesgos
- Sin controles compensatorios, estas excepciones podrian ampliar visibilidad del nucleo IAM.

## Dependencias
- Stored procedures de autenticacion endurecidos.
- Middleware de sesion/autorizacion.
- `seguridad.usp_autorizacion_evaluar`.
- `seguridad.usp_auth_validate_session`.
- `observabilidad.usp_auditoria_autorizacion_crear`.

## Controles compensatorios
- Acceso a sesiones/desafios solo por SP endurecidos.
- Token opaco persistido solo como hash.
- Verificacion de ownership/estado en sesion y desafio MFA.
- Expiracion, reintentos y marcado de uso en desafios MFA.
- Auditoria obligatoria de eventos de seguridad y autorizacion.

## Pendientes
- Extender pruebas de regresion RLS a nuevas tablas piloto de negocio.
- Revisar periodicamente excepciones IAM al ampliar cobertura RLS.

## Autor
Codex (arquitectura IAM SecureERP)

## Estado
Aprobado para Fase 3.2 Hardening Final
