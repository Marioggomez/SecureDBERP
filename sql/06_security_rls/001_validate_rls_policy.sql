SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'seguridad.fn_rls_tenant_empresa', N'IF') IS NULL
BEGIN
    THROW 54100, N'No existe seguridad.fn_rls_tenant_empresa.', 1;
END;

IF NOT EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'RLS_scope_tenant_empresa')
BEGIN
    THROW 54101, N'No existe la policy RLS_scope_tenant_empresa.', 1;
END;

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'RLS_scope_tenant_empresa' AND is_enabled = 0)
BEGIN
    THROW 54102, N'La policy RLS_scope_tenant_empresa esta deshabilitada.', 1;
END;

WITH multiempresa AS (
    SELECT t.object_id,
           QUOTENAME(s.name) + N'.' + QUOTENAME(t.name) AS table_name
    FROM sys.tables t
    JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE EXISTS (SELECT 1 FROM sys.columns c WHERE c.object_id = t.object_id AND c.name = N'id_tenant')
      AND EXISTS (SELECT 1 FROM sys.columns c WHERE c.object_id = t.object_id AND c.name = N'id_empresa')
), rls_targets AS (
    SELECT DISTINCT pred.target_object_id AS object_id
    FROM sys.security_predicates pred
    JOIN sys.security_policies pol ON pol.object_id = pred.object_id
    WHERE pol.name = N'RLS_scope_tenant_empresa'
), missing AS (
    SELECT m.table_name
    FROM multiempresa m
    LEFT JOIN rls_targets r ON r.object_id = m.object_id
    WHERE r.object_id IS NULL
)
SELECT table_name
INTO #missing_multiempresa_rls
FROM missing;

IF EXISTS (SELECT 1 FROM #missing_multiempresa_rls)
BEGIN
    DECLARE @missing nvarchar(max);
    SELECT @missing = STRING_AGG(table_name, N', ') FROM #missing_multiempresa_rls;
    DROP TABLE #missing_multiempresa_rls;
    DECLARE @message nvarchar(2048) = N'Tablas multiempresa sin RLS: ' + @missing;
    THROW 54103, @message, 1;
END;

DROP TABLE #missing_multiempresa_rls;

SELECT CAST(1 AS bit) AS ok,
       N'RLS policy and multiempresa coverage validated.' AS message;
GO
