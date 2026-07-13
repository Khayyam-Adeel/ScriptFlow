-- @Query is always bound as a parameter value, never concatenated into the SQL text, so the
-- LIKE wildcard search introduces no injection risk. Mirrors
-- InMemoryPatientRepository.SearchAsync's contains-match across FirstName/LastName/Nhi.
CREATE OR ALTER PROCEDURE Profile.usp_Patient_Search
    @Query NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        DECLARE @Pattern NVARCHAR(204) = '%' + @Query + '%';

        SELECT
            Id,
            FirstName,
            LastName,
            Address,
            Nhi
        FROM Profile.tblPatients
        WHERE IsDeleted = 0
          AND (FirstName LIKE @Pattern OR LastName LIKE @Pattern OR Nhi LIKE @Pattern);
    END TRY
    BEGIN CATCH
        INSERT INTO dbo.TblErrorLog (ID, Error, StoreProcedure, ErrorStack, InsertedAt)
        VALUES (
            NEWID(),
            LEFT(ERROR_MESSAGE(), 200),
            'Profile.usp_Patient_Search',
            CONCAT('Number=', ERROR_NUMBER(), '; Severity=', ERROR_SEVERITY(), '; State=', ERROR_STATE(),
                   '; Line=', ERROR_LINE(), '; Procedure=', ERROR_PROCEDURE(), '; Message=', ERROR_MESSAGE()),
            SYSUTCDATETIME()
        );

        THROW;
    END CATCH
END
