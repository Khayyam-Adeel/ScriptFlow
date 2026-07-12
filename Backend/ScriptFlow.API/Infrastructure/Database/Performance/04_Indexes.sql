SET QUOTED_IDENTIFIER ON; -- required for the filtered index below
GO

-- Performance chapter, step 4: targeted indexes, added AFTER capturing the "before"
-- execution plans/STATISTICS IO for 03_ReportingQueries.sql against only the baseline
-- single-column indexes from SPEC/DatabaseSpec.md. Run this, then re-capture as "after".
--
-- Rationale per index - see PERFORMANCE.md for the measured before/after numbers this
-- was tuned against, not just theoretical justification:

-- Query 1 (dispensing volumes) filters Status=3 and groups by month(CreatedAtUtc) and
-- PracticeLocationId. The existing single-column IX_Prescriptions_Status can find the
-- Acknowledged rows but then needs a key/RID lookup back to the clustered index for
-- CreatedAtUtc and PracticeLocationId on every matching row. This index covers the whole
-- query (Status equality, CreatedAtUtc read directly, PracticeLocationId via INCLUDE).
CREATE NONCLUSTERED INDEX IX_Prescriptions_Status_CreatedAtUtc
    ON Prescription.tblPrescriptions (Status, CreatedAtUtc)
    INCLUDE (PracticeLocationId);

-- Query 2a/2b group by PracticeLocationId/ProviderId with a Status IN (3,4) filter. The
-- existing single-column indexes on PracticeLocationId/ProviderId don't carry Status, so
-- every matching row needs a lookup just to test the filter. These carry Status directly.
CREATE NONCLUSTERED INDEX IX_Prescriptions_PracticeLocationId_Status
    ON Prescription.tblPrescriptions (PracticeLocationId, Status);

CREATE NONCLUSTERED INDEX IX_Prescriptions_ProviderId_Status
    ON Prescription.tblPrescriptions (ProviderId, Status);

-- Query 3 (repeat-due list) filters to a small, selective slice (Status=3 AND SignedAtUtc
-- older than 90 days) of the 1.2M rows and needs SignedAtUtc for the range predicate and
-- ordering. A filtered index keeps this index small (only Acknowledged rows) instead of
-- indexing all 1.2M rows for a query that only ever wants ~1/6 of them.
CREATE NONCLUSTERED INDEX IX_Prescriptions_Acknowledged_SignedAtUtc
    ON Prescription.tblPrescriptions (SignedAtUtc)
    INCLUDE (PatientId, ProviderId, Scid, RepeatOfPrescriptionId)
    WHERE Status = 3;
