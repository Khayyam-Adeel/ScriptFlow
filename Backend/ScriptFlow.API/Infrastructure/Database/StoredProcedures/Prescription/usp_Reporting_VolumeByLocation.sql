-- Admin overview chart: total prescription volume (all statuses) per practice location,
-- adapted from Performance/03_ReportingQueries.sql's Query 1 (which broke volume down by
-- month too - simplified to an all-time total per location for a single ranked bar chart).
-- Rides IX_Prescriptions_PracticeLocationId_Status (Performance/04_Indexes.sql) for the
-- GROUP BY instead of a full scan of the 1M+ row table.
CREATE OR ALTER PROCEDURE Prescription.usp_Reporting_VolumeByLocation
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        SELECT TOP (20)
            pl.Name AS LocationName,
            COUNT(*) AS Cnt
        FROM Prescription.tblPrescriptions p
        JOIN Admin.tblPracticeLocations pl ON pl.Id = p.PracticeLocationId
        WHERE p.IsDeleted = 0
        GROUP BY pl.Name
        ORDER BY Cnt DESC;
    END TRY
    BEGIN CATCH
        INSERT INTO dbo.TblErrorLog (ID, Error, StoreProcedure, ErrorStack, InsertedAt)
        VALUES (
            NEWID(),
            LEFT(ERROR_MESSAGE(), 200),
            'Prescription.usp_Reporting_VolumeByLocation',
            CONCAT('Number=', ERROR_NUMBER(), '; Severity=', ERROR_SEVERITY(), '; State=', ERROR_STATE(),
                   '; Line=', ERROR_LINE(), '; Procedure=', ERROR_PROCEDURE(), '; Message=', ERROR_MESSAGE()),
            SYSUTCDATETIME()
        );

        THROW;
    END CATCH
END
