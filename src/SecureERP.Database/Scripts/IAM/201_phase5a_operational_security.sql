SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'seguridad.politica_seguridad_operacional', N'U') IS NULL
BEGIN
    CREATE TABLE seguridad.politica_seguridad_operacional
    (
        id_politica_seguridad_operacional BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_politica_seguridad_operacional PRIMARY KEY,
        codigo_accion NVARCHAR(100) NOT NULL,
        ventana_segundos INT NOT NULL,
        max_intentos INT NOT NULL,
        lockout_minutos INT NULL,
        aplica_lockout BIT NOT NULL CONSTRAINT DF_pso_aplica_lockout DEFAULT(0),
        activo BIT NOT NULL CONSTRAINT DF_pso_activo DEFAULT(1),
        creado_utc DATETIME2(7) NOT NULL CONSTRAINT DF_pso_creado_utc DEFAULT(SYSUTCDATETIME()),
        actualizado_utc DATETIME2(7) NULL
    );

    CREATE UNIQUE INDEX UX_politica_seguridad_operacional_codigo
        ON seguridad.politica_seguridad_operacional(codigo_accion);
END;
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'seguridad.contador_rate_limit')
      AND name = N'IX_contador_rate_limit_scope'
)
BEGIN
    CREATE INDEX IX_contador_rate_limit_scope
        ON seguridad.contador_rate_limit(endpoint, ambito, llave, id_tenant, id_empresa, inicio_ventana_utc);
END;
GO

-- RLS exception for seguridad.contador_rate_limit is handled in prior hardening releases.
-- Kept as no-op for idempotent operational releases.
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'seguridad.contador_rate_limit')
      AND name = N'IX_contador_rate_limit_key_lookup'
)
BEGIN
    CREATE INDEX IX_contador_rate_limit_key_lookup
        ON seguridad.contador_rate_limit(ambito, llave, endpoint, id_tenant, id_empresa);
END;
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'seguridad.control_intentos_login')
      AND name = N'IX_control_intentos_login_lookup'
)
BEGIN
    CREATE INDEX IX_control_intentos_login_lookup
        ON seguridad.control_intentos_login(login_usuario, ip);
END;
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'seguridad.politica_ip')
      AND name = N'IX_politica_ip_lookup'
)
BEGIN
    CREATE INDEX IX_politica_ip_lookup
        ON seguridad.politica_ip(id_tenant, id_empresa, ip_o_cidr, accion, activo, prioridad);
END;
GO

IF NOT EXISTS (SELECT 1 FROM seguridad.politica_seguridad_operacional WHERE codigo_accion = N'AUTH.LOGIN')
BEGIN
    INSERT INTO seguridad.politica_seguridad_operacional
    (
        codigo_accion,
        ventana_segundos,
        max_intentos,
        lockout_minutos,
        aplica_lockout,
        activo
    )
    VALUES
    (
        N'AUTH.LOGIN',
        60,
        5,
        5,
        1,
        1
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM seguridad.politica_seguridad_operacional WHERE codigo_accion = N'AUTH.MFA.CHALLENGE')
BEGIN
    INSERT INTO seguridad.politica_seguridad_operacional(codigo_accion, ventana_segundos, max_intentos, lockout_minutos, aplica_lockout, activo)
    VALUES (N'AUTH.MFA.CHALLENGE', 60, 6, NULL, 0, 1);
END;
GO

IF NOT EXISTS (SELECT 1 FROM seguridad.politica_seguridad_operacional WHERE codigo_accion = N'AUTH.MFA.VERIFY')
BEGIN
    INSERT INTO seguridad.politica_seguridad_operacional(codigo_accion, ventana_segundos, max_intentos, lockout_minutos, aplica_lockout, activo)
    VALUES (N'AUTH.MFA.VERIFY', 60, 8, NULL, 0, 1);
END;
GO

IF NOT EXISTS (SELECT 1 FROM seguridad.politica_seguridad_operacional WHERE codigo_accion = N'AUTH.VALIDATE_SESSION')
BEGIN
    INSERT INTO seguridad.politica_seguridad_operacional(codigo_accion, ventana_segundos, max_intentos, lockout_minutos, aplica_lockout, activo)
    VALUES (N'AUTH.VALIDATE_SESSION', 60, 20, NULL, 0, 1);
END;
GO

IF NOT EXISTS (SELECT 1 FROM seguridad.politica_seguridad_operacional WHERE codigo_accion = N'WORKFLOW.APPROVAL_INSTANCE.CREATE')
BEGIN
    INSERT INTO seguridad.politica_seguridad_operacional(codigo_accion, ventana_segundos, max_intentos, lockout_minutos, aplica_lockout, activo)
    VALUES (N'WORKFLOW.APPROVAL_INSTANCE.CREATE', 60, 10, NULL, 0, 1);
END;
GO

IF OBJECT_ID(N'seguridad.usp_security_rate_limit_evaluar', N'P') IS NULL
BEGIN
    EXEC(N'CREATE PROCEDURE seguridad.usp_security_rate_limit_evaluar AS BEGIN SET NOCOUNT ON; END');
END;
GO

CREATE OR ALTER PROCEDURE seguridad.usp_security_rate_limit_evaluar
    @codigo_accion NVARCHAR(100),
    @ambito NVARCHAR(30),
    @llave NVARCHAR(300),
    @id_tenant BIGINT = NULL,
    @id_empresa BIGINT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @now DATETIME2(7) = DATEADD(NANOSECOND, ABS(CHECKSUM(NEWID())) % 1000, SYSUTCDATETIME());
    DECLARE @ventana_segundos INT;
    DECLARE @max_intentos INT;

    SELECT TOP (1)
        @ventana_segundos = p.ventana_segundos,
        @max_intentos = p.max_intentos
    FROM seguridad.politica_seguridad_operacional p
    WHERE p.codigo_accion = @codigo_accion
      AND p.activo = 1;

    IF @ventana_segundos IS NULL OR @max_intentos IS NULL OR @max_intentos <= 0
    BEGIN
        SELECT
            CAST(1 AS BIT) AS permitido,
            CAST(0 AS INT) AS conteo,
            CAST(0 AS INT) AS max_intentos,
            CAST(0 AS INT) AS retry_after_seconds;
        RETURN;
    END;

    DECLARE @id BIGINT;
    DECLARE @inicio DATETIME2(7);
    DECLARE @conteo INT;
    DECLARE @permitido BIT = 1;
    DECLARE @retry_after_seconds INT = 0;

    BEGIN TRANSACTION;

    SELECT TOP (1)
        @id = c.id_contador_rate_limit,
        @inicio = c.inicio_ventana_utc,
        @conteo = c.conteo
    FROM seguridad.contador_rate_limit c WITH (UPDLOCK, HOLDLOCK)
    WHERE c.endpoint = @codigo_accion
      AND c.ambito = @ambito
      AND c.llave = @llave
      AND ISNULL(c.id_tenant, -1) = ISNULL(@id_tenant, -1)
      AND ISNULL(c.id_empresa, -1) = ISNULL(@id_empresa, -1)
    ORDER BY c.id_contador_rate_limit DESC;

    IF @id IS NULL OR DATEADD(SECOND, @ventana_segundos, @inicio) <= @now
    BEGIN
        IF @id IS NULL
        BEGIN
            BEGIN TRY
                INSERT INTO seguridad.contador_rate_limit
                (
                    id_tenant, id_empresa, ambito, llave, endpoint, inicio_ventana_utc, conteo
                )
                VALUES
                (
                    @id_tenant, @id_empresa, @ambito, @llave, @codigo_accion, @now, 1
                );
                SET @conteo = 1;
                SET @inicio = @now;
            END TRY
            BEGIN CATCH
                IF ERROR_NUMBER() NOT IN (2601, 2627) THROW;

                SELECT TOP (1)
                    @id = c.id_contador_rate_limit,
                    @inicio = c.inicio_ventana_utc,
                    @conteo = c.conteo
                FROM seguridad.contador_rate_limit c WITH (UPDLOCK, HOLDLOCK)
                WHERE c.endpoint = @codigo_accion
                  AND c.ambito = @ambito
                  AND c.llave = @llave
                  AND ISNULL(c.id_tenant, -1) = ISNULL(@id_tenant, -1)
                  AND ISNULL(c.id_empresa, -1) = ISNULL(@id_empresa, -1)
                ORDER BY c.id_contador_rate_limit DESC;

                IF @id IS NOT NULL
                BEGIN
                    SET @conteo = ISNULL(@conteo, 0) + 1;
                    UPDATE seguridad.contador_rate_limit
                    SET conteo = @conteo
                    WHERE id_contador_rate_limit = @id;
                END
                ELSE
                BEGIN
                    THROW;
                END
            END CATCH;
        END
        ELSE
        BEGIN
            UPDATE seguridad.contador_rate_limit
            SET inicio_ventana_utc = @now,
                conteo = 1
            WHERE id_contador_rate_limit = @id;
            SET @conteo = 1;
            SET @inicio = @now;
        END
    END
    ELSE
    BEGIN
        SET @conteo = ISNULL(@conteo, 0) + 1;
        UPDATE seguridad.contador_rate_limit
        SET conteo = @conteo
        WHERE id_contador_rate_limit = @id;
    END;

    COMMIT TRANSACTION;

    IF @conteo > @max_intentos
    BEGIN
        SET @permitido = 0;
        SET @retry_after_seconds = DATEDIFF(SECOND, @now, DATEADD(SECOND, @ventana_segundos, @inicio));
        IF @retry_after_seconds < 1 SET @retry_after_seconds = 1;
    END;

    SELECT
        @permitido AS permitido,
        @conteo AS conteo,
        @max_intentos AS max_intentos,
        @retry_after_seconds AS retry_after_seconds;
END;
GO

IF OBJECT_ID(N'seguridad.usp_security_login_lockout_control', N'P') IS NULL
BEGIN
    EXEC(N'CREATE PROCEDURE seguridad.usp_security_login_lockout_control AS BEGIN SET NOCOUNT ON; END');
END;
GO

CREATE OR ALTER PROCEDURE seguridad.usp_security_login_lockout_control
    @login_usuario VARCHAR(240),
    @ip VARCHAR(45),
    @modo VARCHAR(20) -- CHECK | FAILED | SUCCESS
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @now DATETIME2(7) = SYSUTCDATETIME();
    DECLARE @max_intentos INT = 5;
    DECLARE @lockout_minutos INT = 5;

    SELECT TOP (1)
        @max_intentos = p.max_intentos,
        @lockout_minutos = ISNULL(p.lockout_minutos, 5)
    FROM seguridad.politica_seguridad_operacional p
    WHERE p.codigo_accion = N'AUTH.LOGIN'
      AND p.activo = 1;

    DECLARE @id BIGINT;
    DECLARE @intentos INT = 0;
    DECLARE @bloqueado_hasta DATETIME2(7) = NULL;

    SELECT TOP (1)
        @id = c.id_control_intento,
        @intentos = ISNULL(c.intentos, 0),
        @bloqueado_hasta = c.bloqueado_hasta
    FROM seguridad.control_intentos_login c
    WHERE ISNULL(c.login_usuario, '') = ISNULL(@login_usuario, '')
      AND ISNULL(c.ip, '') = ISNULL(@ip, '')
    ORDER BY c.id_control_intento DESC;

    IF @id IS NULL
    BEGIN
        INSERT INTO seguridad.control_intentos_login(login_usuario, ip, intentos, fecha_ultimo_intento, bloqueado_hasta)
        VALUES(@login_usuario, @ip, 0, @now, NULL);
        SET @id = SCOPE_IDENTITY();
        SET @intentos = 0;
        SET @bloqueado_hasta = NULL;
    END;

    IF @modo = 'CHECK'
    BEGIN
        SELECT
            CAST(CASE WHEN @bloqueado_hasta IS NOT NULL AND @bloqueado_hasta > @now THEN 1 ELSE 0 END AS BIT) AS bloqueado,
            @bloqueado_hasta AS bloqueado_hasta,
            @intentos AS intentos;
        RETURN;
    END;

    IF @modo = 'SUCCESS'
    BEGIN
        UPDATE seguridad.control_intentos_login
        SET intentos = 0,
            bloqueado_hasta = NULL,
            fecha_ultimo_intento = @now
        WHERE id_control_intento = @id;

        SELECT CAST(0 AS BIT) AS bloqueado, CAST(NULL AS DATETIME2) AS bloqueado_hasta, 0 AS intentos;
        RETURN;
    END;

    IF @modo = 'FAILED'
    BEGIN
        SET @intentos = @intentos + 1;
        DECLARE @bloqueo_factor INT = CASE
            WHEN @intentos < @max_intentos THEN 0
            WHEN @intentos = @max_intentos THEN 1
            WHEN @intentos <= (@max_intentos + 2) THEN 2
            ELSE 4
        END;

        IF @bloqueo_factor > 0
        BEGIN
            SET @bloqueado_hasta = DATEADD(MINUTE, @lockout_minutos * @bloqueo_factor, @now);
        END
        ELSE
        BEGIN
            SET @bloqueado_hasta = NULL;
        END;

        UPDATE seguridad.control_intentos_login
        SET intentos = @intentos,
            bloqueado_hasta = @bloqueado_hasta,
            fecha_ultimo_intento = @now
        WHERE id_control_intento = @id;

        SELECT
            CAST(CASE WHEN @bloqueado_hasta IS NOT NULL AND @bloqueado_hasta > @now THEN 1 ELSE 0 END AS BIT) AS bloqueado,
            @bloqueado_hasta AS bloqueado_hasta,
            @intentos AS intentos;
        RETURN;
    END;

    THROW 51090, N'Modo no soportado para lockout.', 1;
END;
GO

IF OBJECT_ID(N'seguridad.usp_security_ip_policy_evaluar', N'P') IS NULL
BEGIN
    EXEC(N'CREATE PROCEDURE seguridad.usp_security_ip_policy_evaluar AS BEGIN SET NOCOUNT ON; END');
END;
GO

CREATE OR ALTER PROCEDURE seguridad.usp_security_ip_policy_evaluar
    @id_tenant BIGINT = NULL,
    @id_empresa BIGINT = NULL,
    @ip_origen NVARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @now DATETIME2(7) = SYSUTCDATETIME();

    IF EXISTS
    (
        SELECT 1
        FROM seguridad.ip_bloqueada ib
        WHERE ib.ip = @ip_origen
          AND (ib.fecha_expiracion IS NULL OR ib.fecha_expiracion > @now)
    )
    BEGIN
        SELECT CAST(0 AS BIT) AS permitido, N'IP_BLOCKLIST' AS reason_code;
        RETURN;
    END;

    IF @id_tenant IS NULL
    BEGIN
        SELECT CAST(1 AS BIT) AS permitido, N'NO_TENANT_CONTEXT' AS reason_code;
        RETURN;
    END;

    DECLARE @has_allowlist BIT = CASE
        WHEN EXISTS
        (
            SELECT 1
            FROM seguridad.politica_ip p
            WHERE p.id_tenant = @id_tenant
              AND (p.id_empresa IS NULL OR p.id_empresa = @id_empresa)
              AND p.activo = 1
              AND UPPER(p.accion) = 'ALLOW'
        ) THEN 1 ELSE 0 END;

    IF EXISTS
    (
        SELECT 1
        FROM seguridad.politica_ip p
        WHERE p.id_tenant = @id_tenant
          AND (p.id_empresa IS NULL OR p.id_empresa = @id_empresa)
          AND p.activo = 1
          AND UPPER(p.accion) = 'DENY'
          AND p.ip_o_cidr = @ip_origen
    )
    BEGIN
        SELECT CAST(0 AS BIT) AS permitido, N'IP_POLICY_DENY' AS reason_code;
        RETURN;
    END;

    IF @has_allowlist = 1
    BEGIN
        IF EXISTS
        (
            SELECT 1
            FROM seguridad.politica_ip p
            WHERE p.id_tenant = @id_tenant
              AND (p.id_empresa IS NULL OR p.id_empresa = @id_empresa)
              AND p.activo = 1
              AND UPPER(p.accion) = 'ALLOW'
              AND p.ip_o_cidr = @ip_origen
        )
        BEGIN
            SELECT CAST(1 AS BIT) AS permitido, N'IP_ALLOWLIST_MATCH' AS reason_code;
            RETURN;
        END;

        SELECT CAST(0 AS BIT) AS permitido, N'IP_ALLOWLIST_REQUIRED' AS reason_code;
        RETURN;
    END;

    SELECT CAST(1 AS BIT) AS permitido, N'IP_POLICY_OK' AS reason_code;
END;
GO
