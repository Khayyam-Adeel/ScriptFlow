CREATE OR ALTER PROCEDURE Prescription.usp_Prescription_Create
    @Id                     UNIQUEIDENTIFIER,
    @Scid                   CHAR(11),
    @PatientId              UNIQUEIDENTIFIER,
    @ProviderId             UNIQUEIDENTIFIER,
    @PracticeLocationId     UNIQUEIDENTIFIER,
    @RepeatOfPrescriptionId UNIQUEIDENTIFIER = NULL,
    @Medications            dbo.tvpMedicationLine READONLY,
    @InsertedBy             UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO Prescription.tblPrescriptions
            (Id, Scid, PatientId, ProviderId, PracticeLocationId, RepeatOfPrescriptionId, InsertedBy)
        VALUES
            (@Id, @Scid, @PatientId, @ProviderId, @PracticeLocationId, @RepeatOfPrescriptionId, @InsertedBy);

        INSERT INTO dbo.PrescriptionMedications
            (Id, PrescriptionId, MedicineId, TakeValue, Frequency, Duration, Quantity, Directions,
             Route, Strength, IsPrn, Notes, Repeats, RepeatsUsed, InsertedBy)
        SELECT
            m.Id, @Id, m.MedicineId, m.TakeValue, m.Frequency, m.Duration, m.Quantity, m.Directions,
            m.Route, m.Strength, m.IsPrn, m.Notes, m.Repeats, m.RepeatsUsed, @InsertedBy
        FROM @Medications m;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;

        INSERT INTO dbo.TblErrorLog (ID, Error, StoreProcedure, ErrorStack, InsertedAt)
        VALUES (
            NEWID(),
            LEFT(ERROR_MESSAGE(), 200),
            'Prescription.usp_Prescription_Create',
            CONCAT('Number=', ERROR_NUMBER(), '; Severity=', ERROR_SEVERITY(), '; State=', ERROR_STATE(),
                   '; Line=', ERROR_LINE(), '; Procedure=', ERROR_PROCEDURE(), '; Message=', ERROR_MESSAGE()),
            SYSUTCDATETIME()
        );

        THROW;
    END CATCH
END
