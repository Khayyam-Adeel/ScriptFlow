-- Admin overview chart: rejection rate per provider, adapted from
-- Performance/03_ReportingQueries.sql's Query 2b, capped to the top 10 by rate for a
-- readable bar chart. Rides IX_Prescriptions_ProviderId_Status.
CREATE OR ALTER PROCEDURE Prescription.usp_Reporting_RejectionRateByProvider
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        SELECT TOP (10)
            pr.FirstName + ' ' + pr.LastName AS Name,
            SUM(CASE WHEN p.Status = 4 THEN 1 ELSE 0 END) AS RejectedCount,
            COUNT(*) AS FinalizedCount,
            CAST(100.0 * SUM(CASE WHEN p.Status = 4 THEN 1 ELSE 0 END) / COUNT(*) AS DECIMAL(5, 2)) AS RejectionRatePct
        FROM Prescription.tblPrescriptions p
        JOIN Profile.tblProviders pr ON pr.Id = p.ProviderId
        WHERE p.IsDeleted = 0
          AND p.Status IN (3, 4)
        GROUP BY pr.Id, pr.FirstName, pr.LastName
        ORDER BY RejectionRatePct DESC;
    END TRY
    BEGIN CATCH
        INSERT INTO dbo.TblErrorLog (ID, Error, StoreProcedure, ErrorStack, InsertedAt)
        VALUES (
            NEWID(),
            LEFT(ERROR_MESSAGE(), 200),
            'Prescription.usp_Reporting_RejectionRateByProvider',
            CONCAT('Number=', ERROR_NUMBER(), '; Severity=', ERROR_SEVERITY(), '; State=', ERROR_STATE(),
                   '; Line=', ERROR_LINE(), '; Procedure=', ERROR_PROCEDURE(), '; Message=', ERROR_MESSAGE()),
            SYSUTCDATETIME()
        );

        THROW;
    END CATCH
END
