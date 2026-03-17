# project-references

## título
Matriz de Referencias entre Proyectos

## propósito
Establecer dependencias permitidas para mantener separación de capas y evitar acoplamiento indebido.

## alcance
Aplica a todos los proyectos de la solución SecureERP.sln en esta etapa inicial.

## contexto
El objetivo es evitar sobrearquitectura y, a la vez, prevenir que controladores o frontend absorban lógica de negocio o seguridad de base de datos.

## decisiones
- SecureERP.Api referencia: SecureERP.Application y SecureERP.Infrastructure.
- SecureERP.Application referencia: SecureERP.Domain.
- SecureERP.Infrastructure referencia: SecureERP.Application y SecureERP.Domain.
- SecureERP.Domain no referencia otros proyectos.
- SecureERP.Database no participa en referencias C#.
- SecureERP.Tests referencia Application, Domain e Infrastructure para validaciones base.

## riesgos
- Agregar referencias cruzadas no autorizadas puede romper aislamiento de capas.
- Exponer infraestructura directamente al dominio introduciría deuda técnica temprana.

## dependencias
- Archivo de solución `SecureERP.sln`.
- Archivos `.csproj` de cada proyecto.

## pendientes
- Incorporar reglas automáticas de arquitectura en pipeline CI.
- Revisar periódicamente la matriz al incorporar nuevos paquetes o proyectos.

## autor
Codex (arquitectura inicial bajo lineamientos de Mario R. Gomez)

## estado
Vigente
