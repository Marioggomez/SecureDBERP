SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @required TABLE (schema_name sysname NOT NULL PRIMARY KEY);
INSERT INTO @required(schema_name)
VALUES
    (N'actividad'),
    (N'catalogo'),
    (N'comun'),
    (N'core'),
    (N'cumplimiento'),
    (N'documento'),
    (N'etiqueta'),
    (N'logistica'),
    (N'observabilidad'),
    (N'organizacion'),
    (N'orm'),
    (N'plataforma'),
    (N'platform'),
    (N'security'),
    (N'seguridad'),
    (N'tercero');

DECLARE @missing nvarchar(max);
SELECT @missing = STRING_AGG(r.schema_name, N', ')
FROM @required r
WHERE NOT EXISTS (SELECT 1 FROM sys.schemas s WHERE s.name = r.schema_name);

IF @missing IS NOT NULL
BEGIN
    DECLARE @message nvarchar(2048) = N'Faltan esquemas requeridos: ' + @missing;
    THROW 54010, @message, 1;
END;

SELECT CAST(1 AS bit) AS ok, N'All required schemas exist.' AS message;
GO
