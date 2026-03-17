SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'seguridad.security_event_audit', N'U') IS NOT NULL
AND OBJECT_ID(N'seguridad.fn_rls_tenant_empresa', N'IF') IS NOT NULL
BEGIN
    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.security_predicates sp
        WHERE sp.target_object_id = OBJECT_ID(N'seguridad.security_event_audit')
          AND sp.predicate_type = 0
          AND sp.predicate_definition LIKE N'%fn_rls_tenant_empresa%'
    )
    BEGIN
        ALTER SECURITY POLICY seguridad.RLS_scope_tenant_empresa
        ADD FILTER PREDICATE seguridad.fn_rls_tenant_empresa(id_tenant, id_empresa)
        ON seguridad.security_event_audit;
    END;
END;
GO
