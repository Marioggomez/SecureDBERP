SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.usp_sysdiagrams_listar', N'P') IS NULL EXEC(N'CREATE PROCEDURE dbo.usp_sysdiagrams_listar AS BEGIN SET NOCOUNT ON; END');
IF OBJECT_ID(N'dbo.usp_sysdiagrams_obtener', N'P') IS NULL EXEC(N'CREATE PROCEDURE dbo.usp_sysdiagrams_obtener @diagram_id int AS BEGIN SET NOCOUNT ON; END');
IF OBJECT_ID(N'dbo.usp_sysdiagrams_crear', N'P') IS NULL EXEC(N'CREATE PROCEDURE dbo.usp_sysdiagrams_crear @name nvarchar(128), @principal_id int, @version int, @definition varbinary(max) AS BEGIN SET NOCOUNT ON; END');
IF OBJECT_ID(N'dbo.usp_sysdiagrams_actualizar', N'P') IS NULL EXEC(N'CREATE PROCEDURE dbo.usp_sysdiagrams_actualizar @diagram_id int, @name nvarchar(128), @principal_id int, @version int, @definition varbinary(max) AS BEGIN SET NOCOUNT ON; END');
IF OBJECT_ID(N'dbo.usp_sysdiagrams_desactivar', N'P') IS NULL EXEC(N'CREATE PROCEDURE dbo.usp_sysdiagrams_desactivar @diagram_id int, @usuario varchar(180) = NULL AS BEGIN SET NOCOUNT ON; END');
GO

ALTER PROCEDURE [dbo].[usp_sysdiagrams_listar]
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        THROW 52901, N'Objeto retirado: sysdiagrams no habilitado en este entorno.', 1;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

ALTER PROCEDURE [dbo].[usp_sysdiagrams_obtener]
    @diagram_id int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        THROW 52902, N'Objeto retirado: sysdiagrams no habilitado en este entorno.', 1;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

ALTER PROCEDURE [dbo].[usp_sysdiagrams_crear]
    @name nvarchar(128),
    @principal_id int,
    @version int,
    @definition varbinary(max)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        THROW 52903, N'Objeto retirado: sysdiagrams no habilitado en este entorno.', 1;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

ALTER PROCEDURE [dbo].[usp_sysdiagrams_actualizar]
    @diagram_id int,
    @name nvarchar(128),
    @principal_id int,
    @version int,
    @definition varbinary(max)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        THROW 52904, N'Objeto retirado: sysdiagrams no habilitado en este entorno.', 1;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

ALTER PROCEDURE [dbo].[usp_sysdiagrams_desactivar]
    @diagram_id int,
    @usuario varchar(180) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        THROW 52905, N'Objeto retirado: sysdiagrams no habilitado en este entorno.', 1;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO
