-- Same two-result-set shape as usp_Prescription_GetById, but for every prescription matching
-- the optional filters, replicating InMemoryPrescriptionRepository.ListAsync exactly:
-- both @PatientId and @Status are optional (NULL = no filter on that column).
-- Capped at the 200 most recent matches regardless of filter - this is a list view, not an
-- export; an unfiltered call used to try to return the entire table (1M+ rows after the
-- performance-chapter seed), which is what broke the dashboard (see usp_Prescription_StatusCounts
-- for the actual counts-across-everything the dashboard needs instead).
CREATE OR ALTER PROCEDURE Prescription.usp_Prescription_List
    @PatientId UNIQUEIDENTIFIER = NULL,
    @Status    TINYINT          = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        IF OBJECT_ID('tempdb..#Filtered') IS NOT NULL DROP TABLE #Filtered;

        SELECT TOP (200)
            Id, Scid, PatientId, ProviderId, PracticeLocationId, Status,
            RepeatOfPrescriptionId, CreatedAtUtc, SignedAtUtc
        INTO #Filtered
        FROM Prescription.tblPrescriptions
        WHERE IsDeleted = 0
          AND (@PatientId IS NULL OR PatientId = @PatientId)
          AND (@Status IS NULL OR Status = @Status)
        ORDER BY CreatedAtUtc DESC;

        SELECT * FROM #Filtered ORDER BY CreatedAtUtc DESC;

        SELECT
            pm.Id,
            pm.MedicineId,
            pm.TakeValue,
            pm.Frequency,
            pm.Duration,
            pm.Quantity,
            pm.Directions,
            pm.PrescriptionId
        FROM dbo.PrescriptionMedications pm
        WHERE pm.IsDeleted = 0
          AND pm.PrescriptionId IN (SELECT Id FROM #Filtered);

        DROP TABLE #Filtered;
    END TRY
    BEGIN CATCH
        IF OBJECT_ID('tempdb..#Filtered') IS NOT NULL DROP TABLE #Filtered;

        INSERT INTO dbo.TblErrorLog (ID, Error, StoreProcedure, ErrorStack, InsertedAt)
        VALUES (
            NEWID(),
            LEFT(ERROR_MESSAGE(), 200),
            'Prescription.usp_Prescription_List',
            CONCAT('Number=', ERROR_NUMBER(), '; Severity=', ERROR_SEVERITY(), '; State=', ERROR_STATE(),
                   '; Line=', ERROR_LINE(), '; Procedure=', ERROR_PROCEDURE(), '; Message=', ERROR_MESSAGE()),
            SYSUTCDATETIME()
        );

        THROW;
    END CATCH
END
