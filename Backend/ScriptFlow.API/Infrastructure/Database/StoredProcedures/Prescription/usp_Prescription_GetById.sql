-- Returns two result sets: the prescription header, then its medication lines. Medicine
-- *names* are resolved separately via Lookup.usp_Medicine_GetByIds in C#, matching the
-- current handler flow (GetPrescriptionByIdQueryHandler calls both repositories).
CREATE OR ALTER PROCEDURE Prescription.usp_Prescription_GetById
    @Id UNIQUEIDENTIFIER
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
        WHERE Id = @Id
          AND IsDeleted = 0;

        SELECT
            Id,
            MedicineId,
            TakeValue,
            Frequency,
            Duration,
            Quantity,
            Directions
        FROM dbo.PrescriptionMedications
        WHERE PrescriptionId = @Id
          AND IsDeleted = 0;
    END TRY
    BEGIN CATCH
        INSERT INTO dbo.TblErrorLog (ID, Error, StoreProcedure, ErrorStack, InsertedAt)
        VALUES (
            NEWID(),
            LEFT(ERROR_MESSAGE(), 200),
            'Prescription.usp_Prescription_GetById',
            CONCAT('Number=', ERROR_NUMBER(), '; Severity=', ERROR_SEVERITY(), '; State=', ERROR_STATE(),
                   '; Line=', ERROR_LINE(), '; Procedure=', ERROR_PROCEDURE(), '; Message=', ERROR_MESSAGE()),
            SYSUTCDATETIME()
        );

        THROW;
    END CATCH
END
