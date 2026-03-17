SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

CREATE OR ALTER PROCEDURE seguridad.usp_auth_validate_session
    @token_hash BINARY(32),
    @idle_timeout_minutes INT = 30,
    @actualizar_actividad BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @now DATETIME2(3) = SYSUTCDATETIME();

    ;WITH sesion AS
    (
        SELECT TOP (1)
            s.id_sesion_usuario,
            s.id_usuario,
            s.id_tenant,
            s.id_empresa,
            s.mfa_validado,
            s.expira_absoluta_utc,
            s.ultima_actividad_utc,
            CAST(CASE
                WHEN s.activo = 0 THEN 0
                WHEN s.revocada_utc IS NOT NULL THEN 0
                WHEN s.expira_absoluta_utc < @now THEN 0
                WHEN DATEADD(MINUTE, @idle_timeout_minutes, s.ultima_actividad_utc) < @now THEN 0
                ELSE 1
            END AS BIT) AS sesion_valida
        FROM seguridad.sesion_usuario s
        WHERE s.token_hash = @token_hash
    )
    SELECT * INTO #sesion FROM sesion;

    IF EXISTS (SELECT 1 FROM #sesion WHERE sesion_valida = 1) AND @actualizar_actividad = 1
    BEGIN
        UPDATE s
           SET s.ultima_actividad_utc = @now
        FROM seguridad.sesion_usuario s
        INNER JOIN #sesion x
            ON x.id_sesion_usuario = s.id_sesion_usuario;

        UPDATE #sesion
           SET ultima_actividad_utc = @now;
    END;

    SELECT
        id_sesion_usuario,
        id_usuario,
        id_tenant,
        id_empresa,
        mfa_validado,
        expira_absoluta_utc,
        ultima_actividad_utc,
        sesion_valida
    FROM #sesion;
END;
GO
