CREATE OR ALTER PROCEDURE Profile.usp_Patient_Create
    @Id         UNIQUEIDENTIFIER,
    @FirstName  NVARCHAR(100),
    @LastName   NVARCHAR(100),
    @Address    NVARCHAR(500),
    @Nhi        CHAR(7),
    @InsertedBy UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        INSERT INTO Profile.tblPatients (Id, FirstName, LastName, Address, Nhi, InsertedBy)
        VALUES (@Id, @FirstName, @LastName, @Address, @Nhi, @InsertedBy);
    END TRY
    BEGIN CATCH
        INSERT INTO dbo.TblErrorLog (ID, Error, StoreProcedure, ErrorStack, InsertedAt)
        VALUES (
            NEWID(),
            LEFT(ERROR_MESSAGE(), 200),
            'Profile.usp_Patient_Create',
            CONCAT('Number=', ERROR_NUMBER(), '; Severity=', ERROR_SEVERITY(), '; State=', ERROR_STATE(),
                   '; Line=', ERROR_LINE(), '; Procedure=', ERROR_PROCEDURE(), '; Message=', ERROR_MESSAGE()),
            SYSUTCDATETIME()
        );

        THROW;
    END CATCH
END
