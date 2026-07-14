CREATE OR ALTER PROCEDURE Profile.usp_Provider_List
    @PracticeLocationId UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        SELECT
            Id,
            FirstName,
            LastName,
            Type,
            NzmcNo,
            PracticeLocationId,
            Email,
            PhoneNumber,
            Qualification
        FROM Profile.tblProviders WITH (NOLOCK)
        WHERE IsDeleted = 0
          AND IsActive = 1
          AND (@PracticeLocationId IS NULL OR PracticeLocationId = @PracticeLocationId)
        ORDER BY LastName, FirstName;
    END TRY
    BEGIN CATCH
        INSERT INTO dbo.TblErrorLog (ID, Error, StoreProcedure, ErrorStack, InsertedAt)
        VALUES (
            NEWID(),
            LEFT(ERROR_MESSAGE(), 200),
            'Profile.usp_Provider_List',
            CONCAT('Number=', ERROR_NUMBER(), '; Severity=', ERROR_SEVERITY(), '; State=', ERROR_STATE(),
                   '; Line=', ERROR_LINE(), '; Procedure=', ERROR_PROCEDURE(), '; Message=', ERROR_MESSAGE()),
            SYSUTCDATETIME()
        );

        THROW;
    END CATCH
END
