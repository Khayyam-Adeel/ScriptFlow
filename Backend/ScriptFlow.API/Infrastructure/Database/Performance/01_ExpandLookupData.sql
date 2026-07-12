-- Performance chapter, step 1: bulk up the lookup/master data that the 1M+ prescription
-- seed (02_SeedPrescriptions.sql) fans out across, so grouped reports (dispensing volumes,
-- rejection rate by location/provider) have real variance instead of one giant bucket.
-- Idempotent: only tops up the shortfall each table is missing, safe to re-run.
SET NOCOUNT ON;

DECLARE @SystemUserId UNIQUEIDENTIFIER = '00000000-0000-0000-0000-0000000000AA';

DECLARE @TargetPractices INT = 10;
DECLARE @TargetLocationsPerPractice INT = 3;
DECLARE @TargetProviders INT = 150;
DECLARE @TargetPatients INT = 50000;
DECLARE @TargetMedicines INT = 20;

-- 1. Practices -----------------------------------------------------------------
;WITH Nums AS (
    SELECT TOP (@TargetPractices) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS N
    FROM sys.all_objects a CROSS JOIN sys.all_objects b
)
INSERT INTO Admin.tblPractices (Id, Name, InsertedBy)
SELECT NEWID(), CONCAT('Perf Test Practice ', N), @SystemUserId
FROM Nums
WHERE (SELECT COUNT(*) FROM Admin.tblPractices) < @TargetPractices;

-- 2. Practice locations (N per practice) ----------------------------------------
;WITH Practices AS (
    SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) AS PracticeOrdinal
    FROM Admin.tblPractices
),
Nums AS (
    SELECT TOP (@TargetLocationsPerPractice) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS N
    FROM sys.all_objects
),
Wanted AS (
    SELECT p.Id AS PracticeId, p.PracticeOrdinal, n.N AS LocationOrdinal
    FROM Practices p CROSS JOIN Nums n
)
INSERT INTO Admin.tblPracticeLocations (Id, PracticeId, Name, HpiNo, HpiExtension, InsertedBy)
SELECT
    NEWID(),
    w.PracticeId,
    CONCAT('Location ', w.PracticeOrdinal, '-', w.LocationOrdinal),
    -- Deterministic, collision-free HpiNo: 3 letters derived from ordinal + 2 digits.
    CONCAT(
        CHAR(65 + (w.PracticeOrdinal % 26)),
        CHAR(65 + ((w.PracticeOrdinal * 7) % 26)),
        CHAR(65 + ((w.LocationOrdinal * 3) % 26))
    ) + RIGHT('0' + CAST(w.LocationOrdinal AS VARCHAR(2)), 2),
    'A',
    @SystemUserId
FROM Wanted w
WHERE (SELECT COUNT(*) FROM Admin.tblPracticeLocations) < (@TargetPractices * @TargetLocationsPerPractice);

-- 3. Providers, spread evenly across practice locations -------------------------
;WITH Locations AS (
    SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) AS LocationOrdinal, COUNT(*) OVER () AS LocationCount
    FROM Admin.tblPracticeLocations
),
Nums AS (
    SELECT TOP (@TargetProviders) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS N
    FROM sys.all_objects a CROSS JOIN sys.all_objects b
)
INSERT INTO Profile.tblProviders (Id, FirstName, LastName, Type, NzmcNo, PracticeLocationId, InsertedBy)
SELECT
    NEWID(),
    CONCAT('Provider', n.N),
    'Test',
    0, -- Doctor
    CONCAT('NZMC', RIGHT('000000' + CAST(n.N AS VARCHAR(6)), 6)),
    l.Id,
    @SystemUserId
FROM Nums n
JOIN Locations l ON l.LocationOrdinal = ((n.N - 1) % l.LocationCount) + 1
WHERE (SELECT COUNT(*) FROM Profile.tblProviders) < @TargetProviders;

-- 4. Patients ---------------------------------------------------------------------
;WITH Nums AS (
    SELECT TOP (@TargetPatients) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS N
    FROM sys.all_objects a CROSS JOIN sys.all_objects b
)
INSERT INTO Profile.tblPatients (Id, FirstName, LastName, Address, Nhi, InsertedBy)
SELECT
    NEWID(),
    CONCAT('Patient', N),
    'Test',
    CONCAT(N, ' Test Street'),
    -- NHI: 3 letters + 4 digits, deterministic from N, collision-free up to 26^3 * some spread.
    CONCAT(
        CHAR(65 + (N % 26)),
        CHAR(65 + ((N / 26) % 26)),
        CHAR(65 + ((N / 676) % 26))
    ) + RIGHT('0000' + CAST(N % 10000 AS VARCHAR(4)), 4),
    @SystemUserId
FROM Nums
WHERE (SELECT COUNT(*) FROM Profile.tblPatients) < @TargetPatients;

-- 5. Medicines ---------------------------------------------------------------------
;WITH Nums AS (
    SELECT TOP (@TargetMedicines) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS N
    FROM sys.all_objects
)
INSERT INTO Lookup.tblMedicines (Id, Name, Sctid, Form, InsertedBy)
SELECT NEWID(), CONCAT('Perf Test Medicine ', N), CONCAT('9', RIGHT('0000000' + CAST(N AS VARCHAR(7)), 7)), 'Tablet', @SystemUserId
FROM Nums
WHERE (SELECT COUNT(*) FROM Lookup.tblMedicines) < @TargetMedicines;

SELECT 'Practices' AS TableName, COUNT(*) AS NumRows FROM Admin.tblPractices
UNION ALL SELECT 'PracticeLocations', COUNT(*) FROM Admin.tblPracticeLocations
UNION ALL SELECT 'Providers', COUNT(*) FROM Profile.tblProviders
UNION ALL SELECT 'Patients', COUNT(*) FROM Profile.tblPatients
UNION ALL SELECT 'Medicines', COUNT(*) FROM Lookup.tblMedicines;
