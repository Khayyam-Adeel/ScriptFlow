-- Backs IProviderRepository.GetManyAsync's bulk lookup, used by the prescription list to
-- resolve provider names for the grid without one request per row (mirrors
-- Lookup.usp_Medicine_GetByIds exactly).
CREATE OR ALTER PROCEDURE Profile.usp_Provider_GetByIds
    @Ids dbo.tvpGuidList READONLY
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        SELECT
            pr.Id,
            pr.FirstName,
            pr.LastName,
            pr.Type,
            pr.NzmcNo,
            pr.PracticeLocationId,
            pr.Email,
            pr.PhoneNumber,
            pr.Qualification
        FROM Profile.tblProviders pr
        INNER JOIN @Ids i ON i.Id = pr.Id
        WHERE pr.IsDeleted = 0;
    END TRY
    BEGIN CATCH
        INSERT INTO dbo.TblErrorLog (ID, Error, StoreProcedure, ErrorStack, InsertedAt)
        VALUES (
            NEWID(),
            LEFT(ERROR_MESSAGE(), 200),
            'Profile.usp_Provider_GetByIds',
            CONCAT('Number=', ERROR_NUMBER(), '; Severity=', ERROR_SEVERITY(), '; State=', ERROR_STATE(),
                   '; Line=', ERROR_LINE(), '; Procedure=', ERROR_PROCEDURE(), '; Message=', ERROR_MESSAGE()),
            SYSUTCDATETIME()
        );

        THROW;
    END CATCH
END
