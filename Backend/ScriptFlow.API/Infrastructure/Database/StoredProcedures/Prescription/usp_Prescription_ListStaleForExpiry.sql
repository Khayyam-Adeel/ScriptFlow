-- For PrescriptionExpiryService's periodic sweep - every prescription still in a non-terminal
-- state (Created=0, Signed=1, Dispatched=2) that was created before @OlderThanUtc. Same
-- two-result-set shape as usp_Prescription_List/usp_Prescription_GetById, but deliberately NOT
-- capped with TOP (n): this is an internal sweep that needs every stale row, not a UI page.
-- Expected to stay small in practice (a healthy pipeline moves prescriptions to a terminal
-- state within @StaleAfterHours; a large result here means the pipeline itself is stuck).
-- Rides the existing IX_Prescriptions_Status_CreatedAtUtc index (Status, CreatedAtUtc) for the
-- seek/range-scan; the other selected columns need a key lookup per matching row, which is an
-- accepted trade-off here (an hourly background sweep, not a per-request UI path) rather than
-- adding a fourth covering index to this table just for it.
CREATE OR ALTER PROCEDURE Prescription.usp_Prescription_ListStaleForExpiry
    @OlderThanUtc DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        IF OBJECT_ID('tempdb..#Stale') IS NOT NULL DROP TABLE #Stale;

        SELECT
            Id, Scid, PatientId, ProviderId, PracticeLocationId, Status,
            RepeatOfPrescriptionId, CreatedAtUtc, SignedAtUtc, RejectionReason
        INTO #Stale
        FROM Prescription.tblPrescriptions
        WHERE IsDeleted = 0
          AND Status IN (0, 1, 2)
          AND CreatedAtUtc < @OlderThanUtc;

        SELECT * FROM #Stale ORDER BY CreatedAtUtc ASC;

        SELECT
            pm.Id,
            pm.MedicineId,
            pm.TakeValue,
            pm.Frequency,
            pm.Duration,
            pm.Quantity,
            pm.Directions,
            pm.Route,
            pm.Strength,
            pm.IsPrn,
            pm.Notes,
            pm.PrescriptionId
        FROM dbo.PrescriptionMedications pm
        WHERE pm.IsDeleted = 0
          AND pm.PrescriptionId IN (SELECT Id FROM #Stale);

        DROP TABLE #Stale;
    END TRY
    BEGIN CATCH
        IF OBJECT_ID('tempdb..#Stale') IS NOT NULL DROP TABLE #Stale;

        INSERT INTO dbo.TblErrorLog (ID, Error, StoreProcedure, ErrorStack, InsertedAt)
        VALUES (
            NEWID(),
            LEFT(ERROR_MESSAGE(), 200),
            'Prescription.usp_Prescription_ListStaleForExpiry',
            CONCAT('Number=', ERROR_NUMBER(), '; Severity=', ERROR_SEVERITY(), '; State=', ERROR_STATE(),
                   '; Line=', ERROR_LINE(), '; Procedure=', ERROR_PROCEDURE(), '; Message=', ERROR_MESSAGE()),
            SYSUTCDATETIME()
        );

        THROW;
    END CATCH
END
