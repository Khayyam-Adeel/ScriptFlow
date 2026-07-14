-- @Query is always bound as a parameter value, never concatenated into the SQL text, so the
-- LIKE wildcard search introduces no injection risk. Mirrors
-- InMemoryPatientRepository.SearchAsync's contains-match across FirstName/LastName/Nhi.
-- An empty @Query yields the '%%' pattern, which matches every non-deleted patient - capped at
-- TOP (200) for the same reason as usp_Prescription_List: this is a search/picker view, not an
-- export, and an unfiltered call used to try to return the entire table (50,000+ rows after the
-- performance-chapter seed), which is what made the patients page look like it wasn't loading.
CREATE OR ALTER PROCEDURE Profile.usp_Patient_Search
    @Query NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        DECLARE @Pattern NVARCHAR(204) = '%' + @Query + '%';

        SELECT TOP (200)
            Id,
            FirstName,
            LastName,
            Address,
            Nhi,
            DateOfBirth,
            Gender,
            PhoneNumber,
            Email
        FROM Profile.tblPatients
        WHERE IsDeleted = 0
          AND IsActive = 1
          AND (FirstName LIKE @Pattern OR LastName LIKE @Pattern OR Nhi LIKE @Pattern)
        ORDER BY LastName, FirstName;
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
