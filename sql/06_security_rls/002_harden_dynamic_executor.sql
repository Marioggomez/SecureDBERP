SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'plataforma.usp_exec_sql_if_needed', N'P') IS NULL
BEGIN
    EXEC(N'
CREATE PROCEDURE plataforma.usp_exec_sql_if_needed
    @sql NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
END
');
END;
GO

ALTER PROCEDURE [plataforma].[usp_exec_sql_if_needed]
    @sql NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        DECLARE @sql_norm NVARCHAR(MAX) = LTRIM(RTRIM(ISNULL(@sql, N'')));
        IF @sql_norm = N''
        BEGIN
            SELECT CAST(0 AS bit) AS ejecutado, N'skipped-empty' AS estado;
            RETURN;
        END;

        THROW 52906, N'Bloqueado por seguridad: SQL dinamico generico no permitido.', 1;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO
