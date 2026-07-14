CREATE OR ALTER PROCEDURE Admin.usp_PracticeLocation_List
    @PracticeId UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        SELECT
            Id,
            PracticeId,
            Name,
            HpiNo,
            HpiExtension,
            Address,
            Phone
        FROM Admin.tblPracticeLocations WITH (NOLOCK)
        WHERE IsDeleted = 0
          AND IsActive = 1
          AND (@PracticeId IS NULL OR PracticeId = @PracticeId)
        ORDER BY Name;
    END TRY
    BEGIN CATCH
        INSERT INTO dbo.TblErrorLog (ID, Error, StoreProcedure, ErrorStack, InsertedAt)
        VALUES (
            NEWID(),
            LEFT(ERROR_MESSAGE(), 200),
            'Admin.usp_PracticeLocation_List',
            CONCAT('Number=', ERROR_NUMBER(), '; Severity=', ERROR_SEVERITY(), '; State=', ERROR_STATE(),
                   '; Line=', ERROR_LINE(), '; Procedure=', ERROR_PROCEDURE(), '; Message=', ERROR_MESSAGE()),
            SYSUTCDATETIME()
        );

        THROW;
    END CATCH
END
