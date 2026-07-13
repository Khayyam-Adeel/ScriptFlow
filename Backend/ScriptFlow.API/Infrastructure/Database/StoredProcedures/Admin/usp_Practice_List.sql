CREATE OR ALTER PROCEDURE Admin.usp_Practice_List
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        SELECT
            Id,
            Name
        FROM Admin.tblPractices WITH (NOLOCK)
        WHERE IsDeleted = 0
          AND IsActive = 1
        ORDER BY Name;
    END TRY
    BEGIN CATCH
        INSERT INTO dbo.TblErrorLog (ID, Error, StoreProcedure, ErrorStack, InsertedAt)
        VALUES (
            NEWID(),
            LEFT(ERROR_MESSAGE(), 200),
            'Admin.usp_Practice_List',
            CONCAT('Number=', ERROR_NUMBER(), '; Severity=', ERROR_SEVERITY(), '; State=', ERROR_STATE(),
                   '; Line=', ERROR_LINE(), '; Procedure=', ERROR_PROCEDURE(), '; Message=', ERROR_MESSAGE()),
            SYSUTCDATETIME()
        );

        THROW;
    END CATCH
END
