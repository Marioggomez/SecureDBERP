SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF EXISTS
(
    SELECT 1
    FROM sys.security_predicates pr
    INNER JOIN sys.security_policies p ON p.object_id = pr.object_id
    WHERE p.name = N'RLS_scope_tenant_empresa'
      AND pr.target_object_id = OBJECT_ID(N'seguridad.sesion_usuario')
      AND pr.predicate_type = 0
)
BEGIN
    ALTER SECURITY POLICY seguridad.RLS_scope_tenant_empresa
    DROP FILTER PREDICATE ON seguridad.sesion_usuario;
END;
GO

IF EXISTS
(
    SELECT 1
    FROM sys.security_predicates pr
    INNER JOIN sys.security_policies p ON p.object_id = pr.object_id
    WHERE p.name = N'RLS_scope_tenant_empresa'
      AND pr.target_object_id = OBJECT_ID(N'seguridad.desafio_mfa')
      AND pr.predicate_type = 0
)
BEGIN
    ALTER SECURITY POLICY seguridad.RLS_scope_tenant_empresa
    DROP FILTER PREDICATE ON seguridad.desafio_mfa;
END;
GO
