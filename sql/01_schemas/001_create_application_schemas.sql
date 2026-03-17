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

DECLARE @schema_name sysname;
DECLARE c CURSOR LOCAL FAST_FORWARD FOR
    SELECT r.schema_name
    FROM @required r
    WHERE NOT EXISTS (SELECT 1 FROM sys.schemas s WHERE s.name = r.schema_name)
    ORDER BY r.schema_name;

OPEN c;
FETCH NEXT FROM c INTO @schema_name;

WHILE @@FETCH_STATUS = 0
BEGIN
    DECLARE @sql nvarchar(400) = N'CREATE SCHEMA ' + QUOTENAME(@schema_name) + N' AUTHORIZATION dbo;';
    EXEC sys.sp_executesql @sql;
    FETCH NEXT FROM c INTO @schema_name;
END

CLOSE c;
DEALLOCATE c;

SELECT s.name AS schema_name
FROM sys.schemas s
WHERE s.name IN (SELECT schema_name FROM @required)
ORDER BY s.name;
GO
