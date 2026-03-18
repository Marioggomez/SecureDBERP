SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF NOT EXISTS (SELECT 1 FROM seguridad.permiso WHERE codigo = N'ORGANIZATION.UNIT.READ')
BEGIN
    INSERT INTO seguridad.permiso
    (
        codigo,
        modulo,
        accion,
        nombre,
        descripcion,
        es_sensible,
        activo,
        creado_utc,
        actualizado_utc
    )
    VALUES
    (
        N'ORGANIZATION.UNIT.READ',
        N'Organization',
        N'Read',
        N'Leer unidades organizativas',
        N'Permite listar unidades organizativas por tenant y empresa.',
        0,
        1,
        SYSUTCDATETIME(),
        NULL
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM seguridad.permiso WHERE codigo = N'ORGANIZATION.UNIT.CREATE')
BEGIN
    INSERT INTO seguridad.permiso
    (
        codigo,
        modulo,
        accion,
        nombre,
        descripcion,
        es_sensible,
        activo,
        creado_utc,
        actualizado_utc
    )
    VALUES
    (
        N'ORGANIZATION.UNIT.CREATE',
        N'Organization',
        N'Create',
        N'Crear unidad organizativa',
        N'Permite crear unidades organizativas en el contexto activo.',
        0,
        1,
        SYSUTCDATETIME(),
        NULL
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM seguridad.permiso WHERE codigo = N'WORKFLOW.APPROVAL_INSTANCE.READ')
BEGIN
    INSERT INTO seguridad.permiso
    (
        codigo,
        modulo,
        accion,
        nombre,
        descripcion,
        es_sensible,
        activo,
        creado_utc,
        actualizado_utc
    )
    VALUES
    (
        N'WORKFLOW.APPROVAL_INSTANCE.READ',
        N'Workflow',
        N'Read',
        N'Leer instancias de aprobacion',
        N'Permite consultar instancias de aprobacion en el contexto vigente.',
        0,
        1,
        SYSUTCDATETIME(),
        NULL
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM seguridad.permiso WHERE codigo = N'WORKFLOW.APPROVAL_INSTANCE.CREATE')
BEGIN
    INSERT INTO seguridad.permiso
    (
        codigo,
        modulo,
        accion,
        nombre,
        descripcion,
        es_sensible,
        activo,
        creado_utc,
        actualizado_utc
    )
    VALUES
    (
        N'WORKFLOW.APPROVAL_INSTANCE.CREATE',
        N'Workflow',
        N'Create',
        N'Crear instancia de aprobacion',
        N'Operacion sensible de workflow sujeta a MFA en API.',
        1,
        1,
        SYSUTCDATETIME(),
        NULL
    );
END;
GO
