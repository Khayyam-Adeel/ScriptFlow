-- @Search is always bound as a parameter value, never concatenated into the SQL text, so the
-- LIKE wildcard search introduces no injection risk. NULL/empty @Search returns all active medicines.
CREATE OR ALTER PROCEDURE Lookup.usp_Medicine_List
    @Search NVARCHAR(300) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        DECLARE @Pattern NVARCHAR(304) = '%' + ISNULL(@Search, '') + '%';

        SELECT
            Id,
            Name,
            Sctid,
            Form
        FROM Lookup.tblMedicines WITH (NOLOCK)
        WHERE IsDeleted = 0
          AND IsActive = 1
          AND (@Search IS NULL OR Name LIKE @Pattern OR Sctid LIKE @Pattern)
        ORDER BY Name;
    END TRY
    BEGIN CATCH
        INSERT INTO dbo.TblErrorLog (ID, Error, StoreProcedure, ErrorStack, InsertedAt)
        VALUES (
            NEWID(),
            LEFT(ERROR_MESSAGE(), 200),
            'Lookup.usp_Medicine_List',
            CONCAT('Number=', ERROR_NUMBER(), '; Severity=', ERROR_SEVERITY(), '; State=', ERROR_STATE(),
                   '; Line=', ERROR_LINE(), '; Procedure=', ERROR_PROCEDURE(), '; Message=', ERROR_MESSAGE()),
            SYSUTCDATETIME()
        );

        THROW;
    END CATCH
END
