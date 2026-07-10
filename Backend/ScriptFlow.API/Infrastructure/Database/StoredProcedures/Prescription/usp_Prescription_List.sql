-- Same two-result-set shape as usp_Prescription_GetById, but for every prescription matching
-- the optional filters, replicating InMemoryPrescriptionRepository.ListAsync exactly:
-- both @PatientId and @Status are optional (NULL = no filter on that column).
CREATE OR ALTER PROCEDURE Prescription.usp_Prescription_List
    @PatientId UNIQUEIDENTIFIER = NULL,
    @Status    TINYINT          = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        SELECT
            Id,
            Scid,
            PatientId,
            ProviderId,
            PracticeLocationId,
            Status,
            RepeatOfPrescriptionId,
            CreatedAtUtc,
            SignedAtUtc
        FROM Prescription.tblPrescriptions
        WHERE IsDeleted = 0
          AND (@PatientId IS NULL OR PatientId = @PatientId)
          AND (@Status IS NULL OR Status = @Status)
        ORDER BY CreatedAtUtc DESC;

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
        INNER JOIN Prescription.tblPrescriptions p ON p.Id = pm.PrescriptionId
        WHERE p.IsDeleted = 0
          AND pm.IsDeleted = 0
          AND (@PatientId IS NULL OR p.PatientId = @PatientId)
          AND (@Status IS NULL OR p.Status = @Status);
    END TRY
    BEGIN CATCH
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
