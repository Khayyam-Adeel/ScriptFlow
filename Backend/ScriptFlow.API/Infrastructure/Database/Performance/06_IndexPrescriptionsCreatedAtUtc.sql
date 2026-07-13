SET QUOTED_IDENTIFIER ON; -- required for the filtered index below
GO

-- Performance chapter, step 6: covers the plain UI list view (ScriptFlow-UI's Prescriptions
-- page with no status filter selected, the default state), which none of 04_Indexes.sql's
-- indexes support - those all target reporting queries filtered by Status. The unfiltered
-- case in usp_Prescription_List does:
--   SELECT TOP (200) Id, Scid, PatientId, ProviderId, PracticeLocationId, Status,
--                     RepeatOfPrescriptionId, CreatedAtUtc, SignedAtUtc
--   FROM Prescription.tblPrescriptions WHERE IsDeleted = 0 ORDER BY CreatedAtUtc DESC
-- against 1M+ rows - and the frontend polls this endpoint every 5 seconds, so an inefficient
-- plan here routinely exceeded the default 30s ADO.NET command timeout and 500'd the list page.
--
-- Must be a COVERING index, not just a key on CreatedAtUtc: every row in this table currently
-- has IsDeleted = 0, so a non-covering index would need a key lookup back to the clustered
-- index for almost all 1M+ rows to fetch the other 7 selected columns - the optimizer
-- correctly rejects that in favor of a plain clustered index scan, which is what actually
-- happened when this index first shipped without the INCLUDE list below (verified via
-- SET SHOWPLAN_TEXT: plan was Clustered Index Scan + explicit Sort, index unused). With every
-- selected column available directly from the index, the engine can walk it in CreatedAtUtc
-- DESC order and stop as soon as TOP (200) is satisfied, instead of touching 1M+ rows.
--
-- Filtered on IsDeleted = 0 to match every caller's predicate (and keep the index from growing
-- if soft-deletes start being used) even though it excludes nothing today. Per
-- 05_FixQuotedIdentifierForFilteredIndex.sql, adding a filtered index requires
-- QUOTED_IDENTIFIER ON for every session that subsequently WRITES to this table.
-- usp_Prescription_Create/usp_Prescription_Update were already recompiled with it ON for
-- 04_Indexes.sql's filtered index and verified still set that way
-- (sys.sql_modules.uses_quoted_identifier = 1), so they do not need touching again.
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('Prescription.tblPrescriptions') AND name = 'IX_Prescriptions_CreatedAtUtc')
    DROP INDEX IX_Prescriptions_CreatedAtUtc ON Prescription.tblPrescriptions;
GO

CREATE NONCLUSTERED INDEX IX_Prescriptions_CreatedAtUtc
    ON Prescription.tblPrescriptions (CreatedAtUtc DESC)
    INCLUDE (Scid, PatientId, ProviderId, PracticeLocationId, Status, RepeatOfPrescriptionId, SignedAtUtc)
    WHERE IsDeleted = 0;
GO
