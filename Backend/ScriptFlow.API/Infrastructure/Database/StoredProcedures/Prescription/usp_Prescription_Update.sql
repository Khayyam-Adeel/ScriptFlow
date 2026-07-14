-- Mirrors InMemoryPrescriptionRepository.UpdateAsync's full-overwrite semantics: called both
-- for editing medications (UpdatePrescriptionCommandHandler) and for signing
-- (SignPrescriptionCommandHandler, which re-sends the unchanged medication list alongside the
-- new Status/SignedAtUtc) - so every update replaces the full medication set, even when only
-- the status changed. Consistent with the current repository contract; a narrower
-- "update status only" SP can be split out later if this becomes a perf concern.
CREATE OR ALTER PROCEDURE Prescription.usp_Prescription_Update
    @Id              UNIQUEIDENTIFIER,
    @Status          TINYINT,
    @SignedAtUtc     DATETIME2(3)  = NULL,
    @RejectionReason NVARCHAR(500) = NULL,
    @Medications     dbo.tvpMedicationLine READONLY,
    @UpdatedBy       UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE Prescription.tblPrescriptions
        SET
            Status          = @Status,
            SignedAtUtc     = @SignedAtUtc,
            RejectionReason = @RejectionReason,
            UpdatedAt       = SYSUTCDATETIME(),
            UpdatedBy       = @UpdatedBy
        WHERE Id = @Id;

        DELETE FROM dbo.PrescriptionMedications
        WHERE PrescriptionId = @Id;

        INSERT INTO dbo.PrescriptionMedications
            (Id, PrescriptionId, MedicineId, TakeValue, Frequency, Duration, Quantity, Directions, InsertedBy)
        SELECT
            m.Id, @Id, m.MedicineId, m.TakeValue, m.Frequency, m.Duration, m.Quantity, m.Directions, @UpdatedBy
        FROM @Medications m;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;

        INSERT INTO dbo.TblErrorLog (ID, Error, StoreProcedure, ErrorStack, InsertedAt)
        VALUES (
            NEWID(),
            LEFT(ERROR_MESSAGE(), 200),
            'Prescription.usp_Prescription_Update',
            CONCAT('Number=', ERROR_NUMBER(), '; Severity=', ERROR_SEVERITY(), '; State=', ERROR_STATE(),
                   '; Line=', ERROR_LINE(), '; Procedure=', ERROR_PROCEDURE(), '; Message=', ERROR_MESSAGE()),
            SYSUTCDATETIME()
        );

        THROW;
    END CATCH
END
