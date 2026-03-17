SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

CREATE OR ALTER PROCEDURE seguridad.usp_auth_sesion_marcar_mfa_validado
    @id_sesion_usuario UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE seguridad.sesion_usuario
       SET mfa_validado = 1,
           ultima_actividad_utc = SYSUTCDATETIME()
    WHERE id_sesion_usuario = @id_sesion_usuario
      AND activo = 1
      AND revocada_utc IS NULL;

    SELECT @@ROWCOUNT AS filas_afectadas;
END;
GO
