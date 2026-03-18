# SecureERP Layer Responsibility Contract

## Objetivo
Definir oficialmente que pertenece a cada capa para construir nuevos modulos sin contaminar arquitectura.

## API (transporte)
Permitido:
- Controllers
- Middleware HTTP
- Atributos/filtros HTTP
- Request/response contracts
- Mapping HTTP <-> Application DTO
- Endpoints de health

No permitido:
- ADO.NET (`SqlConnection`, `SqlCommand`, `SqlParameter`)
- Reglas de negocio
- Reglas de dominio
- Persistencia SQL

## Application (casos de uso)
Permitido:
- Handlers / use cases
- Commands / queries
- DTOs de caso de uso
- Orquestacion de seguridad, auditoria y eventos

No permitido:
- ASP.NET MVC / `HttpContext`
- Contratos HTTP
- ADO.NET directo

## Domain (nucleo)
Permitido:
- Entidades
- Value objects
- Invariantes
- Excepciones de dominio
- Interfaces puras de dominio

No permitido:
- ASP.NET
- ADO.NET
- Transporte HTTP
- Dependencias de infraestructura

## Infrastructure (implementacion tecnica)
Permitido:
- Repositorios ADO.NET
- SQL mapping
- Session context appliers
- Implementaciones de servicios tecnicos (hashing, token, mfa code)
- Health checks que dependan de SQL

No permitido:
- Controllers
- Contratos HTTP
- Reglas de negocio de caso de uso

## Database (enforcement operativo)
Permitido:
- SPs
- RLS
- bootstrap
- catalogos operativos
- politicas y enforcement

No permitido:
- Logica de transporte HTTP
- Logica de presentacion

## Regla oficial DTOs
- Api contracts: solo en `SecureERP.Api.Modules.*` (transporte).
- Use-case DTOs: solo en `SecureERP.Application.Modules.*.DTOs`.
- Domain types: solo en `SecureERP.Domain.Modules.*`.

## Regla oficial de seguridad para endpoint nuevo
1. Definir codigo de permiso en catalogo oficial.
2. Agregar constante en `Permissions`.
3. Aplicar `RequirePermission(Permissions.X, requiresMfa: ...)`.
4. Implementar handler en Application.
5. Implementar repositorio/metodo SQL en Infrastructure.
6. Enforce por SP/RLS/politicas en Database.
