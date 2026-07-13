-- For the dashboard's "prescriptions by status" tiles. Cheap regardless of table size - GROUP BY
-- on an already-indexed column - unlike fetching every row just to count them client-side, which
-- is what usp_Prescription_List's unfiltered case used to do (and broke the dashboard once the
-- performance chapter seeded 1M+ rows).
CREATE OR ALTER PROCEDURE Prescription.usp_Prescription_StatusCounts
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        SELECT Status, COUNT(*) AS Cnt
        FROM Prescription.tblPrescriptions
        WHERE IsDeleted = 0
        GROUP BY Status;
    END TRY
    BEGIN CATCH
        INSERT INTO dbo.TblErrorLog (ID, Error, StoreProcedure, ErrorStack, InsertedAt)
        VALUES (
            NEWID(),
            LEFT(ERROR_MESSAGE(), 200),
            'Prescription.usp_Prescription_StatusCounts',
            CONCAT('Number=', ERROR_NUMBER(), '; Severity=', ERROR_SEVERITY(), '; State=', ERROR_STATE(),
                   '; Line=', ERROR_LINE(), '; Procedure=', ERROR_PROCEDURE(), '; Message=', ERROR_MESSAGE()),
            SYSUTCDATETIME()
        );

        THROW;
    END CATCH
END
