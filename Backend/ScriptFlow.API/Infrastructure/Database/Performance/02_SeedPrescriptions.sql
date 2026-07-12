-- Performance chapter, step 2: bulk-seed Prescription.tblPrescriptions (+ matching
-- dbo.PrescriptionMedications) to 1,200,000+ rows using set-based batch inserts (no
-- row-by-row application code - this is orders of magnitude faster than going through
-- the API's stored procedures one prescription at a time).
--
-- Idempotent: if the table is already at/above @TargetTotal, this is a no-op. Re-running
-- after a partial run continues numbering from the current row count, so Scids never
-- collide with a previous run's rows.
--
-- Realism built in for the 3 reporting queries in 03_ReportingQueries.sql:
--   - PracticeLocationId/ProviderId/PatientId/MedicineId are spread evenly across the
--     lookup pools from 01_ExpandLookupData.sql via modulo bucketing.
--   - Each practice location is assigned one of 5 rejection-rate tiers (5/15/25/35/45%),
--     so "rejection rate by practice location/provider" has real variance to report on,
--     not a flat uniform rate.
--   - CreatedAtUtc is spread across the last 24 months for the monthly volumes query.
SET NOCOUNT ON;

DECLARE @SystemUserId UNIQUEIDENTIFIER = '00000000-0000-0000-0000-0000000000AA';
-- Dialed back from an original 1.2M target: this SQL Express instance's small buffer
-- pool means later batches get progressively slower as the table grows past what fits
-- in memory (observed directly during seeding - some 100k-row batches took over 40
-- minutes). 1,050,000 still comfortably clears the "1M+" requirement while cutting the
-- remaining wall-clock time meaningfully.
DECLARE @TargetTotal INT = 1050000;
DECLARE @BatchSize INT = 100000;

DECLARE @CurrentCount INT = (SELECT COUNT(*) FROM Prescription.tblPrescriptions);
DECLARE @ToInsert INT = @TargetTotal - @CurrentCount;

IF @ToInsert <= 0
BEGIN
    PRINT 'Prescription.tblPrescriptions already at or above target; nothing to do.';
    RETURN;
END

-- SQL Server Express's small buffer pool cap means maintaining 5+ non-clustered indexes
-- on every batch insert gets dramatically slower as the table grows past what fits in
-- memory. Standard bulk-load technique: disable them for the load, rebuild once at the
-- end - far cheaper than maintaining them 1.2M times incrementally.
PRINT 'Disabling non-clustered indexes for the bulk load...';
ALTER INDEX UQ_Prescriptions_Scid ON Prescription.tblPrescriptions DISABLE;
ALTER INDEX IX_Prescriptions_PatientId ON Prescription.tblPrescriptions DISABLE;
ALTER INDEX IX_Prescriptions_ProviderId ON Prescription.tblPrescriptions DISABLE;
ALTER INDEX IX_Prescriptions_PracticeLocationId ON Prescription.tblPrescriptions DISABLE;
ALTER INDEX IX_Prescriptions_RepeatOfPrescriptionId ON Prescription.tblPrescriptions DISABLE;
ALTER INDEX IX_Prescriptions_Status ON Prescription.tblPrescriptions DISABLE;
ALTER INDEX IX_PrescriptionMedications_PrescriptionId ON dbo.PrescriptionMedications DISABLE;
ALTER INDEX IX_PrescriptionMedications_MedicineId ON dbo.PrescriptionMedications DISABLE;

PRINT CONCAT('Seeding ', @ToInsert, ' prescriptions, starting from row-number offset ', @CurrentCount, '...');

-- Row-number pool, offset by the current count so Scid numbering never collides with
-- rows a previous run of this script already inserted.
IF OBJECT_ID('tempdb..#Nums') IS NOT NULL DROP TABLE #Nums;
;WITH L0 AS (SELECT 1 AS c UNION ALL SELECT 1),         -- 2
L1 AS (SELECT 1 AS c FROM L0 a CROSS JOIN L0 b),         -- 4
L2 AS (SELECT 1 AS c FROM L1 a CROSS JOIN L1 b),         -- 16
L3 AS (SELECT 1 AS c FROM L2 a CROSS JOIN L2 b),         -- 256
L4 AS (SELECT 1 AS c FROM L3 a CROSS JOIN L3 b),         -- 65,536
L5 AS (SELECT 1 AS c FROM L4 a CROSS JOIN L4 b),         -- ~4.29 billion - plenty for @ToInsert
Nums AS (SELECT ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS N FROM L5)
SELECT TOP (@ToInsert) @CurrentCount + N AS N
INTO #Nums
FROM Nums;
CREATE UNIQUE CLUSTERED INDEX IX_Nums_N ON #Nums (N);

-- Lookup pools, each with a stable ordinal to bucket rows onto via modulo.
IF OBJECT_ID('tempdb..#Locations') IS NOT NULL DROP TABLE #Locations;
SELECT
    Id AS PracticeLocationId,
    ROW_NUMBER() OVER (ORDER BY Id) AS Ordinal,
    COUNT(*) OVER () AS LocationCount,
    5 + ((ROW_NUMBER() OVER (ORDER BY Id) - 1) % 5) * 10 AS RejectPct -- tiers: 5,15,25,35,45
INTO #Locations
FROM Admin.tblPracticeLocations;
CREATE UNIQUE CLUSTERED INDEX IX_Locations_Ordinal ON #Locations (Ordinal);

IF OBJECT_ID('tempdb..#Providers') IS NOT NULL DROP TABLE #Providers;
SELECT Id AS ProviderId, ROW_NUMBER() OVER (ORDER BY Id) AS Ordinal, COUNT(*) OVER () AS ProviderCount
INTO #Providers
FROM Profile.tblProviders;
CREATE UNIQUE CLUSTERED INDEX IX_Providers_Ordinal ON #Providers (Ordinal);

IF OBJECT_ID('tempdb..#Patients') IS NOT NULL DROP TABLE #Patients;
SELECT Id AS PatientId, ROW_NUMBER() OVER (ORDER BY Id) AS Ordinal, COUNT(*) OVER () AS PatientCount
INTO #Patients
FROM Profile.tblPatients;
CREATE UNIQUE CLUSTERED INDEX IX_Patients_Ordinal ON #Patients (Ordinal);

IF OBJECT_ID('tempdb..#Medicines') IS NOT NULL DROP TABLE #Medicines;
SELECT Id AS MedicineId, ROW_NUMBER() OVER (ORDER BY Id) AS Ordinal, COUNT(*) OVER () AS MedicineCount
INTO #Medicines
FROM Lookup.tblMedicines;
CREATE UNIQUE CLUSTERED INDEX IX_Medicines_Ordinal ON #Medicines (Ordinal);

DECLARE @BatchStart INT = @CurrentCount;
DECLARE @ThisBatchEnd INT;

WHILE @BatchStart < @TargetTotal
BEGIN
    SET @ThisBatchEnd = CASE WHEN @BatchStart + @BatchSize > @TargetTotal THEN @TargetTotal ELSE @BatchStart + @BatchSize END;

    IF OBJECT_ID('tempdb..#Batch') IS NOT NULL DROP TABLE #Batch;

    ;WITH Raw AS (
        SELECT
            n.N,
            loc.PracticeLocationId,
            loc.RejectPct,
            prov.ProviderId,
            pat.PatientId,
            med.MedicineId,
            ABS(CHECKSUM(NEWID())) % 100 AS StatusRoll,
            ABS(CHECKSUM(NEWID())) % 730 AS DaysAgo,
            ABS(CHECKSUM(NEWID())) % 1440 AS MinutesToSign
        FROM #Nums n
        JOIN #Locations loc ON loc.Ordinal = ((n.N - 1) % loc.LocationCount) + 1
        JOIN #Providers prov ON prov.Ordinal = ((n.N - 1) % prov.ProviderCount) + 1
        JOIN #Patients pat ON pat.Ordinal = ((n.N - 1) % pat.PatientCount) + 1
        JOIN #Medicines med ON med.Ordinal = ((n.N - 1) % med.MedicineCount) + 1
        WHERE n.N > @BatchStart AND n.N <= @ThisBatchEnd
    )
    SELECT
        NEWID() AS Id,
        CONCAT('9PRF', RIGHT('0000000' + CAST(N AS VARCHAR(7)), 7)) AS Scid,
        PatientId, ProviderId, PracticeLocationId, MedicineId,
        CASE
            WHEN StatusRoll < 5 THEN 0                                    -- Created
            WHEN StatusRoll < 13 THEN 1                                   -- Signed
            WHEN StatusRoll < 17 THEN 2                                   -- Dispatched
            WHEN (StatusRoll - 17) < (83 * RejectPct / 100) THEN 4        -- Rejected
            ELSE 3                                                        -- Acknowledged
        END AS Status,
        DATEADD(DAY, -1 * DaysAgo, SYSUTCDATETIME()) AS CreatedAtUtc,
        MinutesToSign
    INTO #Batch
    FROM Raw;

    INSERT INTO Prescription.tblPrescriptions
        (Id, Scid, PatientId, ProviderId, PracticeLocationId, Status, CreatedAtUtc, SignedAtUtc, InsertedBy)
    SELECT
        Id, Scid, PatientId, ProviderId, PracticeLocationId, Status,
        CreatedAtUtc,
        CASE WHEN Status = 0 THEN NULL ELSE DATEADD(MINUTE, MinutesToSign, CreatedAtUtc) END,
        @SystemUserId
    FROM #Batch;

    INSERT INTO dbo.PrescriptionMedications
        (Id, PrescriptionId, MedicineId, TakeValue, Frequency, Duration, Quantity, Directions, InsertedBy)
    SELECT NEWID(), Id, MedicineId, '1 tablet', 'Twice daily', '7 days', 14, 'Take with food', @SystemUserId
    FROM #Batch;

    PRINT CONCAT('  ...inserted through row ', @ThisBatchEnd, ' of ', @TargetTotal);

    SET @BatchStart = @ThisBatchEnd;
    CHECKPOINT;
END

DROP TABLE #Batch;

PRINT 'Rebuilding non-clustered indexes now that the bulk load is done...';
ALTER INDEX UQ_Prescriptions_Scid ON Prescription.tblPrescriptions REBUILD;
ALTER INDEX IX_Prescriptions_PatientId ON Prescription.tblPrescriptions REBUILD;
ALTER INDEX IX_Prescriptions_ProviderId ON Prescription.tblPrescriptions REBUILD;
ALTER INDEX IX_Prescriptions_PracticeLocationId ON Prescription.tblPrescriptions REBUILD;
ALTER INDEX IX_Prescriptions_RepeatOfPrescriptionId ON Prescription.tblPrescriptions REBUILD;
ALTER INDEX IX_Prescriptions_Status ON Prescription.tblPrescriptions REBUILD;
ALTER INDEX IX_PrescriptionMedications_PrescriptionId ON dbo.PrescriptionMedications REBUILD;
ALTER INDEX IX_PrescriptionMedications_MedicineId ON dbo.PrescriptionMedications REBUILD;

SELECT COUNT(*) AS FinalPrescriptionCount FROM Prescription.tblPrescriptions;
