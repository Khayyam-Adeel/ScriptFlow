-- Companion fix for 04_Indexes.sql's filtered index (IX_Prescriptions_Acknowledged_SignedAtUtc,
-- WHERE Status = 3): adding ANY filtered index (or indexed view / computed column) to a table
-- requires QUOTED_IDENTIFIER ON for every session that subsequently writes to that table -
-- not just for the CREATE INDEX statement itself. A stored procedure's QUOTED_IDENTIFIER
-- setting is baked in at CREATE/ALTER time and does not change afterwards regardless of the
-- caller's session settings.
--
-- usp_Prescription_Create and usp_Prescription_Update - the only two procedures that write to
-- Prescription.tblPrescriptions - were both originally compiled with QUOTED_IDENTIFIER OFF.
-- The result: from the moment 04_Indexes.sql's filtered index was added, every prescription
-- create and sign call started failing with:
--   "INSERT failed because the following SET options have incorrect settings: 'QUOTED_IDENTIFIER'."
-- This was caught by the primary-workflow integration test (see TEST_PLAN.md) on its first real
-- run - a genuine regression, not a theoretical one. Run this immediately after 04_Indexes.sql
-- on any database where that filtered index exists.
--
-- Note: written as CREATE OR ALTER (matches the source-of-truth procedure files under
-- StoredProcedures/, for a SQL Server 2016 SP1+ target such as the SQL Server 2022 container in
-- docker-compose.yml). If applying this directly against an older instance (e.g. SQL Server 2014,
-- which predates CREATE OR ALTER), swap each CREATE OR ALTER PROCEDURE for
-- `IF OBJECT_ID(...) IS NOT NULL DROP PROCEDURE ...;` followed by a plain CREATE PROCEDURE.

SET QUOTED_IDENTIFIER ON;
GO

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
            (Id, PrescriptionId, MedicineId, TakeValue, Frequency, Duration, Quantity, Directions, InsertedBy)
        SELECT
            m.Id, @Id, m.MedicineId, m.TakeValue, m.Frequency, m.Duration, m.Quantity, m.Directions, @InsertedBy
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
GO

SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE Prescription.usp_Prescription_Update
    @Id          UNIQUEIDENTIFIER,
    @Status      TINYINT,
    @SignedAtUtc DATETIME2(3) = NULL,
    @Medications dbo.tvpMedicationLine READONLY,
    @UpdatedBy   UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE Prescription.tblPrescriptions
        SET
            Status      = @Status,
            SignedAtUtc = @SignedAtUtc,
            UpdatedAt   = SYSUTCDATETIME(),
            UpdatedBy   = @UpdatedBy
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
GO
