-- Adds Prescription.tblPrescriptions.RejectionReason, for an already-deployed database (a fresh
-- one gets this column directly from 00_CreateSchema.sql). Previously the pharmacy's rejection
-- reason lived only in the transient PrescriptionRejectedEvent - Prescription.Reject() applied
-- the Status change but threw the reason away, so nothing could ever show a prescriber *why*
-- their prescription was rejected. See Prescription.cs's Reject(string reason).
--
-- Idempotent: safe to re-run.
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('Prescription.tblPrescriptions') AND name = 'RejectionReason'
)
BEGIN
    ALTER TABLE Prescription.tblPrescriptions ADD RejectionReason NVARCHAR(500) NULL;
END
GO
