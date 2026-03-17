# SecureERP - Paquete para Evaluacion Externa (Fase 1.1)

## Instruccion para el evaluador
Actua como revisor senior de arquitectura SQL Server enterprise.
Evalua la calidad del inventario tecnico y la clasificacion de Stored Procedures.
No generes codigo. No propongas endpoints.
Responde solo con hallazgos, riesgos, omisiones y correcciones sugeridas de clasificacion.

## Contexto
- Proyecto: SecureERP
- Motor: SQL Server
- Fecha de validacion: 2026-03-17
- Alcance: Fase 1.1 (validacion muestral sobre BD real)

## Reglas de baseline que deben respetarse
1. SP con `SET NOCOUNT ON`
2. SP con `SET XACT_ABORT ON`
3. SP con `TRY/CATCH`
4. Uso de `SESSION_CONTEXT` para contexto
5. Validacion tenant/empresa/usuario/sesion
6. Auditoria en operaciones criticas
7. Sin SQL dinamico inseguro
8. Sin duplicacion de logica de seguridad
9. RLS oficial con `seguridad.fn_rls_tenant_empresa`

## Resultado de inventario (global)
- Esquemas funcionales con objetos: actividad, catalogo, comun, cumplimiento, documento, etiqueta, logistica, observabilidad, organizacion, plataforma, seguridad, tercero.
- Objetos:
  - Tablas: 172
  - Vistas: 3 (todas en seguridad)
  - Funciones: 8 (todas en seguridad)
  - SP: 547
- Distribucion SP:
  - seguridad: 233
  - catalogo: 100
  - plataforma: 71
  - cumplimiento: 41
  - observabilidad: 35
  - tercero: 36
  - organizacion: 26
  - dbo: 5

## Resultado de endurecimiento (global)
- SP con NOCOUNT: 547/547
- SP con XACT_ABORT: 0/547
- SP con TRY/CATCH: 0/547
- SP con SESSION_CONTEXT: 193/547
- SP con mencion de auditoria: 25/547

## Resultado de clasificacion global
- reutilizable: 0
- reutilizable con endurecimiento: 538
- obsoleto/no usar: 7
- duplicado potencial: 2
- faltante por cobertura tabla->SP (naming): 69

## SP marcados como obsoleto/no usar
- dbo.usp_sysdiagrams_actualizar
- dbo.usp_sysdiagrams_crear
- dbo.usp_sysdiagrams_desactivar
- dbo.usp_sysdiagrams_listar
- dbo.usp_sysdiagrams_obtener
- plataforma.usp_exec_sql_if_needed
- seguridad.SP_rls_scope_prepared

## SP marcados como duplicado potencial
- seguridad.usp_preferencia_ui_usuario_guardar
- seguridad.usp_preferencia_ui_usuario_obtener

## Validacion muestral Fase 1.1 (40 SP)
Muestra priorizada:
- 10 seguridad critica
- 10 cumplimiento/workflow
- 10 observabilidad/auditoria
- 5 lectura/listado
- 5 catalogo/soporte

Resultado muestral:
- reutilizable: 0
- reutilizable con endurecimiento: 38
- obsoleto/no usar: 1
- duplicado potencial: 1

Metricas muestrales:
- TRY/CATCH: 0/40
- XACT_ABORT: 0/40
- transaccion explicita: 0/40
- SESSION_CONTEXT: 21/40

## Evidencia RLS oficial
- Policy: seguridad.RLS_scope_tenant_empresa (habilitada)
- Funcion de predicado: seguridad.fn_rls_tenant_empresa
- Tablas protegidas: 52
- Tablas multiempresa (id_tenant + id_empresa) sin RLS: 0

Tablas con BLOCK+FILTER:
- actividad.comentario
- actividad.comentario_archivo
- actividad.comentario_mencion
- cumplimiento.instancia_aprobacion
- seguridad.asignacion_rol_usuario
- seguridad.excepcion_permiso_usuario
- seguridad.filtro_dato_usuario
- seguridad.usuario_empresa
- seguridad.usuario_scope_empresa
- seguridad.usuario_scope_unidad
- seguridad.usuario_scope_usuario
- seguridad.usuario_unidad_organizativa

Tablas con FILTER (resto):
- cumplimiento.accion_instancia_aprobacion
- cumplimiento.auditoria_operacion
- cumplimiento.excepcion_sod
- cumplimiento.perfil_aprobacion
- cumplimiento.regla_sod
- documento.documento
- documento.documento_entidad
- documento.documento_etiqueta
- documento.documento_firma
- documento.documento_ocr
- documento.documento_version
- documento.documento_workflow
- documento.documento_workflow_paso
- etiqueta.etiqueta_entidad
- logistica.contrato_tarifario
- logistica.cotizacion
- logistica.envio
- logistica.factor_volumetrico
- logistica.recargo_regla
- logistica.regla_control_carga
- logistica.ruta_logistica
- logistica.tarifa
- logistica.temporada_tarifaria
- observabilidad.auditoria_autorizacion
- observabilidad.auditoria_evento_seguridad
- observabilidad.auditoria_reinicio_mesa_ayuda
- observabilidad.error_aplicacion
- organizacion.empresa
- organizacion.grupo_empresarial_empresa
- organizacion.unidad_organizativa
- plataforma.configuracion_empresa
- seguridad.configuracion_canal_notificacion
- seguridad.contador_rate_limit
- seguridad.desafio_mfa
- seguridad.politica_empresa_override
- seguridad.politica_ip
- seguridad.preferencia_usuario_ui
- seguridad.sesion_usuario
- tercero.tercero_empresa
- tercero.tercero_rol

## Matriz inicial (extracto)
- Seguridad -> seguridad.sesion_usuario -> seguridad.usp_auth_crear_sesion_usuario -> Riesgo Alto -> Prioridad P1
- Seguridad -> seguridad.flujo_autenticacion -> seguridad.usp_auth_marcar_flujo_autenticacion_mfa_validado -> Riesgo Alto -> Prioridad P1
- Cumplimiento -> cumplimiento.instancia_aprobacion -> cumplimiento.usp_instancia_aprobacion_crear -> Riesgo Alto -> Prioridad P1
- Cumplimiento -> cumplimiento.paso_perfil_aprobacion -> cumplimiento.usp_paso_perfil_aprobacion_crear -> Riesgo Alto -> Prioridad P1
- Auditoria -> cumplimiento.auditoria_operacion -> cumplimiento.usp_auditoria_operacion_registrar -> Riesgo Alto -> Prioridad P1
- Observabilidad -> observabilidad.auditoria_evento_seguridad -> observabilidad.usp_auditoria_evento_seguridad_crear -> Riesgo Alto -> Prioridad P1
- Soporte -> (dinamico) -> plataforma.usp_exec_sql_if_needed -> Riesgo Critico -> Prioridad P1
- Soporte UI -> seguridad.preferencia_usuario_ui (indirecto) -> seguridad.usp_preferencia_ui_usuario_guardar -> Riesgo Medio -> Prioridad P3

## Solicitud puntual de evaluacion (responder en este orden)
1. Validar si la clasificacion global (0/538/7/2) es consistente con la evidencia.
2. Indicar si algun SP de la muestra deberia cambiar de categoria y por que.
3. Identificar riesgos no detectados u omisiones en la Fase 1.1.
4. Confirmar si la cobertura RLS reportada es suficiente bajo el baseline.
5. Priorizar top 10 acciones de endurecimiento (sin crear SP nuevos).
