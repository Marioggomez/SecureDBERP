SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF NOT EXISTS (SELECT 1 FROM catalogo.estado_usuario WHERE codigo = 'ACTIVO')
BEGIN
    SET IDENTITY_INSERT catalogo.estado_usuario ON;
    INSERT INTO catalogo.estado_usuario
    (
        id_estado_usuario,
        codigo,
        nombre,
        descripcion,
        orden_visual,
        activo,
        creado_utc,
        actualizado_utc
    )
    VALUES
    (
        1,
        'ACTIVO',
        N'Activo',
        N'Estado activo',
        1,
        1,
        SYSUTCDATETIME(),
        NULL
    );
    SET IDENTITY_INSERT catalogo.estado_usuario OFF;
END;
GO

IF NOT EXISTS (SELECT 1 FROM catalogo.estado_empresa WHERE codigo = 'ACTIVA')
BEGIN
    SET IDENTITY_INSERT catalogo.estado_empresa ON;
    INSERT INTO catalogo.estado_empresa
    (
        id_estado_empresa,
        codigo,
        nombre,
        descripcion,
        orden_visual,
        activo,
        creado_utc,
        actualizado_utc
    )
    VALUES
    (
        1,
        'ACTIVA',
        N'Activa',
        N'Estado activo',
        1,
        1,
        SYSUTCDATETIME(),
        NULL
    );
    SET IDENTITY_INSERT catalogo.estado_empresa OFF;
END;
GO

IF NOT EXISTS (SELECT 1 FROM catalogo.tipo_empresa WHERE codigo = 'GENERAL')
BEGIN
    SET IDENTITY_INSERT catalogo.tipo_empresa ON;
    INSERT INTO catalogo.tipo_empresa
    (
        id_tipo_empresa,
        codigo,
        nombre,
        descripcion,
        orden_visual,
        activo,
        creado_utc,
        actualizado_utc
    )
    VALUES
    (
        1,
        'GENERAL',
        N'General',
        N'Tipo general',
        1,
        1,
        SYSUTCDATETIME(),
        NULL
    );
    SET IDENTITY_INSERT catalogo.tipo_empresa OFF;
END;
GO

IF NOT EXISTS (SELECT 1 FROM catalogo.efecto_permiso WHERE codigo = 'ALLOW')
BEGIN
    SET IDENTITY_INSERT catalogo.efecto_permiso ON;
    INSERT INTO catalogo.efecto_permiso
    (
        id_efecto_permiso,
        codigo,
        nombre,
        descripcion,
        orden_visual,
        activo,
        creado_utc,
        actualizado_utc
    )
    VALUES
    (
        1,
        'ALLOW',
        N'Allow',
        N'Permitir',
        1,
        1,
        SYSUTCDATETIME(),
        NULL
    );
    SET IDENTITY_INSERT catalogo.efecto_permiso OFF;
END;
GO

IF NOT EXISTS (SELECT 1 FROM catalogo.efecto_permiso WHERE codigo = 'DENY')
BEGIN
    INSERT INTO catalogo.efecto_permiso
    (
        codigo,
        nombre,
        descripcion,
        orden_visual,
        activo,
        creado_utc,
        actualizado_utc
    )
    VALUES
    (
        'DENY',
        N'Deny',
        N'Denegar',
        2,
        1,
        SYSUTCDATETIME(),
        NULL
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM catalogo.alcance_asignacion WHERE codigo = 'EMPRESA')
BEGIN
    SET IDENTITY_INSERT catalogo.alcance_asignacion ON;
    INSERT INTO catalogo.alcance_asignacion
    (
        id_alcance_asignacion,
        codigo,
        nombre,
        descripcion,
        orden_visual,
        activo,
        creado_utc,
        actualizado_utc
    )
    VALUES
    (
        1,
        'EMPRESA',
        N'Empresa',
        N'Alcance por empresa',
        1,
        1,
        SYSUTCDATETIME(),
        NULL
    );
    SET IDENTITY_INSERT catalogo.alcance_asignacion OFF;
END;
GO

IF NOT EXISTS (SELECT 1 FROM catalogo.proposito_desafio_mfa WHERE codigo = 'LOGIN')
BEGIN
    SET IDENTITY_INSERT catalogo.proposito_desafio_mfa ON;
    INSERT INTO catalogo.proposito_desafio_mfa
    (
        id_proposito_desafio_mfa,
        codigo,
        nombre,
        descripcion,
        orden_visual,
        activo,
        creado_utc,
        actualizado_utc
    )
    VALUES
    (
        1,
        'LOGIN',
        N'Login',
        N'MFA en inicio de sesion',
        1,
        1,
        SYSUTCDATETIME(),
        NULL
    );
    SET IDENTITY_INSERT catalogo.proposito_desafio_mfa OFF;
END;
GO

IF NOT EXISTS (SELECT 1 FROM catalogo.proposito_desafio_mfa WHERE codigo = 'STEP_UP')
BEGIN
    INSERT INTO catalogo.proposito_desafio_mfa
    (
        codigo,
        nombre,
        descripcion,
        orden_visual,
        activo,
        creado_utc,
        actualizado_utc
    )
    VALUES
    (
        'STEP_UP',
        N'StepUp',
        N'MFA para operacion sensible',
        2,
        1,
        SYSUTCDATETIME(),
        NULL
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM catalogo.canal_notificacion WHERE codigo = 'TOTP')
BEGIN
    SET IDENTITY_INSERT catalogo.canal_notificacion ON;
    INSERT INTO catalogo.canal_notificacion
    (
        id_canal_notificacion,
        codigo,
        nombre,
        descripcion,
        orden_visual,
        activo,
        creado_utc,
        actualizado_utc
    )
    VALUES
    (
        1,
        'TOTP',
        N'TOTP',
        N'Aplicacion autenticadora',
        1,
        1,
        SYSUTCDATETIME(),
        NULL
    );
    SET IDENTITY_INSERT catalogo.canal_notificacion OFF;
END;
GO

IF NOT EXISTS (SELECT 1 FROM catalogo.canal_notificacion WHERE codigo = 'EMAIL_OTP')
BEGIN
    INSERT INTO catalogo.canal_notificacion
    (
        codigo,
        nombre,
        descripcion,
        orden_visual,
        activo,
        creado_utc,
        actualizado_utc
    )
    VALUES
    (
        'EMAIL_OTP',
        N'Email OTP',
        N'OTP por correo',
        2,
        1,
        SYSUTCDATETIME(),
        NULL
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM catalogo.canal_notificacion WHERE codigo = 'RECOVERY_CODE')
BEGIN
    INSERT INTO catalogo.canal_notificacion
    (
        codigo,
        nombre,
        descripcion,
        orden_visual,
        activo,
        creado_utc,
        actualizado_utc
    )
    VALUES
    (
        'RECOVERY_CODE',
        N'Recovery code',
        N'Codigo de recuperacion',
        3,
        1,
        SYSUTCDATETIME(),
        NULL
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM catalogo.canal_notificacion WHERE codigo = 'SMS_OTP_FALLBACK')
BEGIN
    INSERT INTO catalogo.canal_notificacion
    (
        codigo,
        nombre,
        descripcion,
        orden_visual,
        activo,
        creado_utc,
        actualizado_utc
    )
    VALUES
    (
        'SMS_OTP_FALLBACK',
        N'SMS OTP fallback',
        N'OTP por SMS solo fallback',
        4,
        1,
        SYSUTCDATETIME(),
        NULL
    );
END;
GO
