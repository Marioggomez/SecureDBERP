SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

CREATE OR ALTER FUNCTION seguridad.fn_rls_tenant_empresa_unidad
(
    @id_tenant BIGINT,
    @id_empresa BIGINT,
    @id_unidad_organizativa BIGINT
)
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN
SELECT 1 AS fn_result
WHERE
    @id_tenant = TRY_CAST(SESSION_CONTEXT(N'id_tenant') AS BIGINT)
    AND
    (
        TRY_CAST(SESSION_CONTEXT(N'es_admin_tenant') AS BIT) = 1
        OR
        (
            @id_empresa = TRY_CAST(SESSION_CONTEXT(N'id_empresa') AS BIGINT)
            AND
            (
                NOT EXISTS
                (
                    SELECT 1
                    FROM seguridad.usuario_scope_unidad usu
                    WHERE usu.id_usuario = TRY_CAST(SESSION_CONTEXT(N'id_usuario') AS BIGINT)
                      AND usu.id_tenant = @id_tenant
                      AND (usu.id_empresa IS NULL OR usu.id_empresa = @id_empresa)
                      AND usu.activo = 1
                )
                OR
                EXISTS
                (
                    SELECT 1
                    FROM seguridad.usuario_scope_unidad usu
                    WHERE usu.id_usuario = TRY_CAST(SESSION_CONTEXT(N'id_usuario') AS BIGINT)
                      AND usu.id_tenant = @id_tenant
                      AND (usu.id_empresa IS NULL OR usu.id_empresa = @id_empresa)
                      AND usu.id_unidad_organizativa = @id_unidad_organizativa
                      AND usu.activo = 1
                )
            )
        )
    );
GO

IF OBJECT_ID(N'cumplimiento.instancia_aprobacion', N'U') IS NOT NULL
AND OBJECT_ID(N'seguridad.fn_rls_tenant_empresa_unidad', N'IF') IS NOT NULL
AND EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'RLS_scope_tenant_empresa' AND SCHEMA_NAME(schema_id) = N'seguridad')
BEGIN
    BEGIN TRY
        EXEC sys.sp_executesql N'ALTER SECURITY POLICY seguridad.RLS_scope_tenant_empresa DROP FILTER PREDICATE ON cumplimiento.instancia_aprobacion;';
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() <> 33261
        BEGIN
            THROW;
        END
    END CATCH;

    BEGIN TRY
        EXEC sys.sp_executesql N'ALTER SECURITY POLICY seguridad.RLS_scope_tenant_empresa DROP BLOCK PREDICATE ON cumplimiento.instancia_aprobacion AFTER INSERT;';
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() <> 33261
        BEGIN
            THROW;
        END
    END CATCH;

    BEGIN TRY
        EXEC sys.sp_executesql N'ALTER SECURITY POLICY seguridad.RLS_scope_tenant_empresa DROP BLOCK PREDICATE ON cumplimiento.instancia_aprobacion AFTER UPDATE;';
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() <> 33261
        BEGIN
            THROW;
        END
    END CATCH;
END;
GO

IF OBJECT_ID(N'cumplimiento.instancia_aprobacion', N'U') IS NOT NULL
AND OBJECT_ID(N'seguridad.fn_rls_tenant_empresa_unidad', N'IF') IS NOT NULL
AND EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'RLS_scope_tenant_empresa' AND SCHEMA_NAME(schema_id) = N'seguridad')
BEGIN
    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.security_predicates pr
        INNER JOIN sys.security_policies p ON p.object_id = pr.object_id
        WHERE p.name = N'RLS_scope_tenant_empresa'
          AND SCHEMA_NAME(p.schema_id) = N'seguridad'
          AND pr.target_object_id = OBJECT_ID(N'cumplimiento.instancia_aprobacion')
          AND pr.predicate_type_desc = N'FILTER'
          AND pr.predicate_definition LIKE N'%fn_rls_tenant_empresa_unidad%'
    )
    BEGIN
        ALTER SECURITY POLICY seguridad.RLS_scope_tenant_empresa
        ADD FILTER PREDICATE seguridad.fn_rls_tenant_empresa_unidad(id_tenant, id_empresa, id_unidad_organizativa)
        ON cumplimiento.instancia_aprobacion;
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.security_predicates pr
        INNER JOIN sys.security_policies p ON p.object_id = pr.object_id
        WHERE p.name = N'RLS_scope_tenant_empresa'
          AND SCHEMA_NAME(p.schema_id) = N'seguridad'
          AND pr.target_object_id = OBJECT_ID(N'cumplimiento.instancia_aprobacion')
          AND pr.predicate_type_desc = N'BLOCK'
          AND pr.operation_desc = N'AFTER INSERT'
          AND pr.predicate_definition LIKE N'%fn_rls_tenant_empresa_unidad%'
    )
    BEGIN
        ALTER SECURITY POLICY seguridad.RLS_scope_tenant_empresa
        ADD BLOCK PREDICATE seguridad.fn_rls_tenant_empresa_unidad(id_tenant, id_empresa, id_unidad_organizativa)
        ON cumplimiento.instancia_aprobacion AFTER INSERT;
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.security_predicates pr
        INNER JOIN sys.security_policies p ON p.object_id = pr.object_id
        WHERE p.name = N'RLS_scope_tenant_empresa'
          AND SCHEMA_NAME(p.schema_id) = N'seguridad'
          AND pr.target_object_id = OBJECT_ID(N'cumplimiento.instancia_aprobacion')
          AND pr.predicate_type_desc = N'BLOCK'
          AND pr.operation_desc = N'AFTER UPDATE'
          AND pr.predicate_definition LIKE N'%fn_rls_tenant_empresa_unidad%'
    )
    BEGIN
        ALTER SECURITY POLICY seguridad.RLS_scope_tenant_empresa
        ADD BLOCK PREDICATE seguridad.fn_rls_tenant_empresa_unidad(id_tenant, id_empresa, id_unidad_organizativa)
        ON cumplimiento.instancia_aprobacion AFTER UPDATE;
    END;
END;
GO
