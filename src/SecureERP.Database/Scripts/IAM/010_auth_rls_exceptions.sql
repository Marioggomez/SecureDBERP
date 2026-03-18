SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

/*
IAM Core Exception: seguridad.sesion_usuario
- This table stores the opaque-session lifecycle used by login, MFA, session validation, logout, and revocation.
- Keeping tenant+company FILTER PREDICATE on this table can deadlock the auth pipeline before final company context is resolved.
- Compensating controls:
  1) Access only through hardened auth procedures.
  2) Token is opaque and only token_hash is persisted.
  3) Session ownership is validated in auth/authorization SPs.
  4) All critical auth decisions are audited (security_event_audit + auditoria_autorizacion).
*/
IF EXISTS
(
    SELECT 1
    FROM sys.security_predicates pr
    INNER JOIN sys.security_policies p ON p.object_id = pr.object_id
    WHERE p.name = N'RLS_scope_tenant_empresa'
      AND SCHEMA_NAME(p.schema_id) = N'seguridad'
      AND pr.target_object_id = OBJECT_ID(N'seguridad.sesion_usuario')
      AND pr.predicate_type_desc = N'FILTER'
)
BEGIN
    DECLARE @sql_drop_rls_sesion NVARCHAR(MAX) =
        N'ALTER SECURITY POLICY seguridad.RLS_scope_tenant_empresa DROP FILTER PREDICATE ON seguridad.sesion_usuario;';
    BEGIN TRY
        EXEC sys.sp_executesql @sql_drop_rls_sesion;
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() <> 33261
        BEGIN
            THROW;
        END
    END CATCH
END;
GO

/*
IAM Core Exception: seguridad.desafio_mfa
- This table stores MFA challenges tied to auth_flow (login) and session (step-up).
- Login MFA starts before select-company; strict tenant+company filter can block valid challenge verification.
- Compensating controls:
  1) Challenge ids are GUIDs with short expiration and max attempts.
  2) Challenge verify flow enforces purpose (Login/StepUp), state, and ownership checks.
  3) Challenge usage is audited through security events.
*/
IF EXISTS
(
    SELECT 1
    FROM sys.security_predicates pr
    INNER JOIN sys.security_policies p ON p.object_id = pr.object_id
    WHERE p.name = N'RLS_scope_tenant_empresa'
      AND SCHEMA_NAME(p.schema_id) = N'seguridad'
      AND pr.target_object_id = OBJECT_ID(N'seguridad.desafio_mfa')
      AND pr.predicate_type_desc = N'FILTER'
)
BEGIN
    DECLARE @sql_drop_rls_desafio NVARCHAR(MAX) =
        N'ALTER SECURITY POLICY seguridad.RLS_scope_tenant_empresa DROP FILTER PREDICATE ON seguridad.desafio_mfa;';
    BEGIN TRY
        EXEC sys.sp_executesql @sql_drop_rls_desafio;
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() <> 33261
        BEGIN
            THROW;
        END
    END CATCH
END;
GO
