-- Admin overview chart: rejection rate per practice location, adapted verbatim from
-- Performance/03_ReportingQueries.sql's Query 2a. Only counts finalized prescriptions
-- (Acknowledged=3 or Rejected=4) - still-pending ones haven't been decided yet and would
-- understate the rate. Rides IX_Prescriptions_PracticeLocationId_Status.
CREATE OR ALTER PROCEDURE Prescription.usp_Reporting_RejectionRateByLocation
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        SELECT
            pl.Name AS Name,
            SUM(CASE WHEN p.Status = 4 THEN 1 ELSE 0 END) AS RejectedCount,
            COUNT(*) AS FinalizedCount,
            CAST(100.0 * SUM(CASE WHEN p.Status = 4 THEN 1 ELSE 0 END) / COUNT(*) AS DECIMAL(5, 2)) AS RejectionRatePct
        FROM Prescription.tblPrescriptions p
        JOIN Admin.tblPracticeLocations pl ON pl.Id = p.PracticeLocationId
        WHERE p.IsDeleted = 0
          AND p.Status IN (3, 4)
        GROUP BY pl.Name
        ORDER BY RejectionRatePct DESC;
    END TRY
    BEGIN CATCH
        INSERT INTO dbo.TblErrorLog (ID, Error, StoreProcedure, ErrorStack, InsertedAt)
        VALUES (
            NEWID(),
            LEFT(ERROR_MESSAGE(), 200),
            'Prescription.usp_Reporting_RejectionRateByLocation',
            CONCAT('Number=', ERROR_NUMBER(), '; Severity=', ERROR_SEVERITY(), '; State=', ERROR_STATE(),
                   '; Line=', ERROR_LINE(), '; Procedure=', ERROR_PROCEDURE(), '; Message=', ERROR_MESSAGE()),
            SYSUTCDATETIME()
        );

        THROW;
    END CATCH
END
