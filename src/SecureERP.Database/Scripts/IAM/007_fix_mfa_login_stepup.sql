SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

CREATE OR ALTER PROCEDURE seguridad.usp_auth_crear_desafio_mfa
    @id_desafio_mfa UNIQUEIDENTIFIER,
    @id_usuario BIGINT,
    @id_tenant BIGINT,
    @id_empresa BIGINT = NULL,
    @id_sesion_usuario UNIQUEIDENTIFIER = NULL,
    @id_flujo_autenticacion UNIQUEIDENTIFIER = NULL,
    @id_proposito_desafio_mfa SMALLINT,
    @id_canal_notificacion SMALLINT,
    @codigo_accion NVARCHAR(100) = NULL,
    @otp_hash BINARY(32),
    @otp_salt VARBINARY(16),
    @expira_en_utc DATETIME2,
    @max_intentos SMALLINT
AS
BEGIN
    BEGIN TRY
        BEGIN TRAN;
        SET NOCOUNT ON;
        SET XACT_ABORT ON;

        DECLARE @ctx_id_usuario BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_usuario') AS BIGINT);
        DECLARE @ctx_id_tenant BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_tenant') AS BIGINT);
        DECLARE @ctx_id_empresa BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_empresa') AS BIGINT);
        DECLARE @ctx_id_sesion UNIQUEIDENTIFIER = TRY_CAST(SESSION_CONTEXT(N'id_sesion') AS UNIQUEIDENTIFIER);

        IF @ctx_id_usuario IS NULL OR @ctx_id_tenant IS NULL
            THROW 51130, N'SESSION_CONTEXT incompleto.', 1;

        IF @id_usuario IS NULL SET @id_usuario = @ctx_id_usuario;
        IF @id_tenant IS NULL SET @id_tenant = @ctx_id_tenant;

        IF @id_usuario <> @ctx_id_usuario
            THROW 51132, N'Conflicto de contexto: id_usuario.', 1;
        IF @id_tenant <> @ctx_id_tenant
            THROW 51133, N'Conflicto de contexto: id_tenant.', 1;

        IF @id_proposito_desafio_mfa = 1
        BEGIN
            IF @id_flujo_autenticacion IS NULL
                THROW 51140, N'AuthFlow requerido para MFA de login.', 1;

            IF NOT EXISTS
            (
                SELECT 1
                FROM seguridad.flujo_autenticacion fa
                WHERE fa.id_flujo_autenticacion = @id_flujo_autenticacion
                  AND fa.id_usuario = @id_usuario
                  AND fa.id_tenant = @id_tenant
                  AND fa.usado = 0
                  AND fa.expira_en_utc >= SYSUTCDATETIME()
            )
                THROW 51141, N'AuthFlow invalido para MFA de login.', 1;

            SET @id_empresa = COALESCE(@id_empresa, @ctx_id_empresa);
            SET @id_sesion_usuario = NULL;
        END
        ELSE IF @id_proposito_desafio_mfa = 2
        BEGIN
            IF @ctx_id_empresa IS NULL OR @ctx_id_sesion IS NULL
                THROW 51142, N'Session context requerido para MFA StepUp.', 1;

            IF @id_empresa IS NULL SET @id_empresa = @ctx_id_empresa;
            IF @id_empresa <> @ctx_id_empresa
                THROW 51134, N'Conflicto de contexto: id_empresa.', 1;

            SET @id_sesion_usuario = @ctx_id_sesion;
            SET @id_flujo_autenticacion = NULL;
        END
        ELSE
        BEGIN
            THROW 51143, N'Proposito MFA no soportado.', 1;
        END

        INSERT INTO seguridad.desafio_mfa
        (
            id_desafio_mfa,
            id_usuario,
            id_tenant,
            id_empresa,
            id_sesion_usuario,
            id_flujo_autenticacion,
            id_proposito_desafio_mfa,
            id_canal_notificacion,
            codigo_accion,
            otp_hash,
            otp_salt,
            expira_en_utc,
            usado,
            intentos,
            max_intentos,
            creado_utc,
            validado_utc
        )
        VALUES
        (
            @id_desafio_mfa,
            @id_usuario,
            @id_tenant,
            @id_empresa,
            @id_sesion_usuario,
            @id_flujo_autenticacion,
            @id_proposito_desafio_mfa,
            @id_canal_notificacion,
            @codigo_accion,
            @otp_hash,
            @otp_salt,
            @expira_en_utc,
            0,
            0,
            @max_intentos,
            SYSUTCDATETIME(),
            NULL
        );

        SELECT @id_desafio_mfa AS id;

        DECLARE @__aud_id_usuario BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_usuario') AS BIGINT);
        DECLARE @__aud_id_sesion UNIQUEIDENTIFIER = TRY_CAST(SESSION_CONTEXT(N'id_sesion') AS UNIQUEIDENTIFIER);
        DECLARE @__aud_usuario VARCHAR(100) = CASE WHEN @__aud_id_usuario IS NULL THEN NULL ELSE CAST(@__aud_id_usuario AS VARCHAR(100)) END;
        EXEC cumplimiento.usp_auditoria_operacion_registrar
            @tabla = 'seguridad.usp_auth_crear_desafio_mfa',
            @operacion = 'EXECUTE',
            @id_registro = NULL,
            @valores_anteriores = NULL,
            @valores_nuevos = NULL,
            @usuario = @__aud_usuario,
            @correlation_id = @__aud_id_sesion,
            @id_usuario = @__aud_id_usuario,
            @id_sesion_usuario = @__aud_id_sesion,
            @ip_origen = NULL,
            @agente_usuario = NULL,
            @resultado = N'OK',
            @origen_aplicacion = N'seguridad.usp_auth_crear_desafio_mfa';

        COMMIT TRAN;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRAN;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE seguridad.usp_auth_obtener_desafio_mfa
    @id_desafio_mfa UNIQUEIDENTIFIER
AS
BEGIN
    BEGIN TRY
        SET NOCOUNT ON;
        SET XACT_ABORT ON;

        SELECT
            id_desafio_mfa,
            id_flujo_autenticacion,
            id_sesion_usuario,
            id_usuario,
            id_tenant,
            id_empresa,
            id_proposito_desafio_mfa,
            otp_hash,
            otp_salt,
            expira_en_utc,
            usado,
            intentos,
            max_intentos
        FROM seguridad.desafio_mfa
        WHERE id_desafio_mfa = @id_desafio_mfa;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRAN;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE seguridad.usp_auth_incrementar_intento_desafio_mfa
    @id_desafio_mfa UNIQUEIDENTIFIER
AS
BEGIN
    BEGIN TRY
        BEGIN TRAN;
        SET NOCOUNT ON;
        SET XACT_ABORT ON;

        DECLARE @ctx_id_tenant BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_tenant') AS BIGINT);
        DECLARE @ctx_id_empresa BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_empresa') AS BIGINT);

        IF @ctx_id_tenant IS NULL
            THROW 51050, N'Scope id_tenant no disponible en SESSION_CONTEXT.', 1;

        UPDATE seguridad.desafio_mfa
        SET intentos = intentos + 1
        WHERE id_desafio_mfa = @id_desafio_mfa
          AND id_tenant = @ctx_id_tenant
          AND (
                (@ctx_id_empresa IS NULL AND id_empresa IS NULL)
                OR id_empresa = @ctx_id_empresa
              )
          AND usado = 0;

        SELECT @@ROWCOUNT AS filas_afectadas;

        DECLARE @__aud_id_usuario BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_usuario') AS BIGINT);
        DECLARE @__aud_id_sesion UNIQUEIDENTIFIER = TRY_CAST(SESSION_CONTEXT(N'id_sesion') AS UNIQUEIDENTIFIER);
        DECLARE @__aud_usuario VARCHAR(100) = CASE WHEN @__aud_id_usuario IS NULL THEN NULL ELSE CAST(@__aud_id_usuario AS VARCHAR(100)) END;
        EXEC cumplimiento.usp_auditoria_operacion_registrar
            @tabla = 'seguridad.usp_auth_incrementar_intento_desafio_mfa',
            @operacion = 'EXECUTE',
            @id_registro = NULL,
            @valores_anteriores = NULL,
            @valores_nuevos = NULL,
            @usuario = @__aud_usuario,
            @correlation_id = @__aud_id_sesion,
            @id_usuario = @__aud_id_usuario,
            @id_sesion_usuario = @__aud_id_sesion,
            @ip_origen = NULL,
            @agente_usuario = NULL,
            @resultado = N'OK',
            @origen_aplicacion = N'seguridad.usp_auth_incrementar_intento_desafio_mfa';

        COMMIT TRAN;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRAN;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE seguridad.usp_auth_marcar_desafio_mfa_validado
    @id_desafio_mfa UNIQUEIDENTIFIER
AS
BEGIN
    BEGIN TRY
        BEGIN TRAN;
        SET NOCOUNT ON;
        SET XACT_ABORT ON;

        DECLARE @ctx_id_tenant BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_tenant') AS BIGINT);
        DECLARE @ctx_id_empresa BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_empresa') AS BIGINT);

        IF @ctx_id_tenant IS NULL
            THROW 51050, N'Scope id_tenant no disponible en SESSION_CONTEXT.', 1;

        UPDATE seguridad.desafio_mfa
        SET
            usado = 1,
            validado_utc = SYSUTCDATETIME()
        WHERE id_desafio_mfa = @id_desafio_mfa
          AND id_tenant = @ctx_id_tenant
          AND (
                (@ctx_id_empresa IS NULL AND id_empresa IS NULL)
                OR id_empresa = @ctx_id_empresa
              )
          AND usado = 0;

        SELECT @@ROWCOUNT AS filas_afectadas;

        DECLARE @__aud_id_usuario BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_usuario') AS BIGINT);
        DECLARE @__aud_id_sesion UNIQUEIDENTIFIER = TRY_CAST(SESSION_CONTEXT(N'id_sesion') AS UNIQUEIDENTIFIER);
        DECLARE @__aud_usuario VARCHAR(100) = CASE WHEN @__aud_id_usuario IS NULL THEN NULL ELSE CAST(@__aud_id_usuario AS VARCHAR(100)) END;
        EXEC cumplimiento.usp_auditoria_operacion_registrar
            @tabla = 'seguridad.usp_auth_marcar_desafio_mfa_validado',
            @operacion = 'EXECUTE',
            @id_registro = NULL,
            @valores_anteriores = NULL,
            @valores_nuevos = NULL,
            @usuario = @__aud_usuario,
            @correlation_id = @__aud_id_sesion,
            @id_usuario = @__aud_id_usuario,
            @id_sesion_usuario = @__aud_id_sesion,
            @ip_origen = NULL,
            @agente_usuario = NULL,
            @resultado = N'OK',
            @origen_aplicacion = N'seguridad.usp_auth_marcar_desafio_mfa_validado';

        COMMIT TRAN;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRAN;
        THROW;
    END CATCH
END;
GO
