-- For the dashboard's volume trend chart - prescriptions created per day, bounded to a recent
-- window (@Since, set by GetPrescriptionDailyVolumeQueryHandler) rather than a full-table GROUP
-- BY like usp_Prescription_StatusCounts. Bounding by @Since lets this ride the existing
-- IX_Prescriptions_CreatedAtUtc covering index (CreatedAtUtc DESC, filtered IsDeleted = 0) as an
-- index seek + range scan instead of touching all 1M+ rows.
CREATE OR ALTER PROCEDURE Prescription.usp_Prescription_DailyVolume
    @Since DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        SELECT CAST(CreatedAtUtc AS DATE) AS CreatedDate, COUNT(*) AS Cnt
        FROM Prescription.tblPrescriptions
        WHERE IsDeleted = 0
          AND CreatedAtUtc >= @Since
        GROUP BY CAST(CreatedAtUtc AS DATE);
    END TRY
    BEGIN CATCH
        INSERT INTO dbo.TblErrorLog (ID, Error, StoreProcedure, ErrorStack, InsertedAt)
        VALUES (
            NEWID(),
            LEFT(ERROR_MESSAGE(), 200),
            'Prescription.usp_Prescription_DailyVolume',
            CONCAT('Number=', ERROR_NUMBER(), '; Severity=', ERROR_SEVERITY(), '; State=', ERROR_STATE(),
                   '; Line=', ERROR_LINE(), '; Procedure=', ERROR_PROCEDURE(), '; Message=', ERROR_MESSAGE()),
            SYSUTCDATETIME()
        );

        THROW;
    END CATCH
END
