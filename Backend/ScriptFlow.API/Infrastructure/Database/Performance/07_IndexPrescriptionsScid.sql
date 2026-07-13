SET QUOTED_IDENTIFIER ON; -- required for the filtered index below
GO

-- Supports the Prescriptions tab's new SCID filter (usp_Prescription_List's @ScidPrefix param):
--   ... AND (@ScidPrefix IS NULL OR Scid LIKE @ScidPrefix + '%') ...
-- Matched as a prefix, not a contains-match, specifically so this stays index-seekable at 1M+
-- rows instead of degrading to a full scan - the same "prefer index-seekable filters" approach
-- 06_IndexPrescriptionsCreatedAtUtc.sql took for the unfiltered list view.
--
-- Covering, for the same reason as 06's index: every row currently has IsDeleted = 0, so a
-- non-covering index would need a key lookup back to the clustered index for almost every
-- matching row to fetch the other 7 selected columns. Filtered on IsDeleted = 0 to match the
-- proc's predicate. The write procs (usp_Prescription_Create/usp_Prescription_Update) were
-- already recompiled with QUOTED_IDENTIFIER ON for 04_Indexes.sql's filtered index (see
-- 05_FixQuotedIdentifierForFilteredIndex.sql) and verified still set that way, so adding this
-- second filtered index needs no further proc changes.
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('Prescription.tblPrescriptions') AND name = 'IX_Prescriptions_Scid')
    DROP INDEX IX_Prescriptions_Scid ON Prescription.tblPrescriptions;
GO

CREATE NONCLUSTERED INDEX IX_Prescriptions_Scid
    ON Prescription.tblPrescriptions (Scid)
    INCLUDE (PatientId, ProviderId, PracticeLocationId, Status, RepeatOfPrescriptionId, CreatedAtUtc, SignedAtUtc)
    WHERE IsDeleted = 0;
GO
