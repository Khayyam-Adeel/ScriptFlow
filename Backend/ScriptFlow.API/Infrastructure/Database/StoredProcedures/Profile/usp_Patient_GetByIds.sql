-- Backs IPatientRepository.GetManyAsync's bulk lookup, used by the prescription list to
-- resolve patient names for the grid without one request per row (mirrors
-- Lookup.usp_Medicine_GetByIds exactly).
CREATE OR ALTER PROCEDURE Profile.usp_Patient_GetByIds
    @Ids dbo.tvpGuidList READONLY
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        SELECT
            p.Id,
            p.FirstName,
            p.LastName,
            p.Address,
            p.Nhi,
            p.DateOfBirth,
            p.Gender,
            p.PhoneNumber,
            p.Email
        FROM Profile.tblPatients p
        INNER JOIN @Ids i ON i.Id = p.Id
        WHERE p.IsDeleted = 0;
    END TRY
    BEGIN CATCH
        INSERT INTO dbo.TblErrorLog (ID, Error, StoreProcedure, ErrorStack, InsertedAt)
        VALUES (
            NEWID(),
            LEFT(ERROR_MESSAGE(), 200),
            'Profile.usp_Patient_GetByIds',
            CONCAT('Number=', ERROR_NUMBER(), '; Severity=', ERROR_SEVERITY(), '; State=', ERROR_STATE(),
                   '; Line=', ERROR_LINE(), '; Procedure=', ERROR_PROCEDURE(), '; Message=', ERROR_MESSAGE()),
            SYSUTCDATETIME()
        );

        THROW;
    END CATCH
END
