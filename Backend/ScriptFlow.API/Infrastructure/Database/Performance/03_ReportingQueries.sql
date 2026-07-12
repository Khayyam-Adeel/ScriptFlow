-- Performance chapter, step 3: the three reporting queries.
--
-- Note on "rejection rates by pharmacy" (from the spec's example list): this schema has
-- no pharmacy/dispensary entity - PharmacyGateway.mock simulates one external, unnamed
-- pharmacy, not multiple named pharmacies with their own identity in the database.
-- Adapted (deliberate reinterpretation, not a silent substitution) to rejection rate by
-- practice location and by provider - both are real, already-indexed dimensions that
-- exist in the schema today.

-- ============================================================================
-- Query 1: Dispensing volumes - count of Acknowledged (dispensed) prescriptions
-- per practice location per calendar month.
-- ============================================================================
SELECT
    pl.Name AS PracticeLocation,
    DATEFROMPARTS(YEAR(p.CreatedAtUtc), MONTH(p.CreatedAtUtc), 1) AS MonthStart,
    COUNT(*) AS DispensedCount
FROM Prescription.tblPrescriptions p
JOIN Admin.tblPracticeLocations pl ON pl.Id = p.PracticeLocationId
WHERE p.Status = 3 -- Acknowledged
GROUP BY pl.Name, DATEFROMPARTS(YEAR(p.CreatedAtUtc), MONTH(p.CreatedAtUtc), 1)
ORDER BY MonthStart, PracticeLocation;

-- ============================================================================
-- Query 2a: Rejection rate by practice location (of finalized prescriptions only -
-- Acknowledged + Rejected; still-pending Created/Signed/Dispatched are excluded since
-- they haven't been decided yet and would understate the rate).
-- ============================================================================
SELECT
    pl.Name AS PracticeLocation,
    SUM(CASE WHEN p.Status = 4 THEN 1 ELSE 0 END) AS RejectedCount,
    COUNT(*) AS FinalizedCount,
    CAST(100.0 * SUM(CASE WHEN p.Status = 4 THEN 1 ELSE 0 END) / COUNT(*) AS DECIMAL(5,2)) AS RejectionRatePct
FROM Prescription.tblPrescriptions p
JOIN Admin.tblPracticeLocations pl ON pl.Id = p.PracticeLocationId
WHERE p.Status IN (3, 4) -- Acknowledged or Rejected
GROUP BY pl.Name
ORDER BY RejectionRatePct DESC;

-- ============================================================================
-- Query 2b: Rejection rate by provider (same shape, different dimension).
-- ============================================================================
SELECT
    pr.FirstName + ' ' + pr.LastName AS Provider,
    SUM(CASE WHEN p.Status = 4 THEN 1 ELSE 0 END) AS RejectedCount,
    COUNT(*) AS FinalizedCount,
    CAST(100.0 * SUM(CASE WHEN p.Status = 4 THEN 1 ELSE 0 END) / COUNT(*) AS DECIMAL(5,2)) AS RejectionRatePct
FROM Prescription.tblPrescriptions p
JOIN Profile.tblProviders pr ON pr.Id = p.ProviderId
WHERE p.Status IN (3, 4)
GROUP BY pr.Id, pr.FirstName, pr.LastName
ORDER BY RejectionRatePct DESC;

-- ============================================================================
-- Query 3: Repeat-due list - Acknowledged prescriptions signed more than 90 days ago
-- that have no existing repeat yet (no other prescription's RepeatOfPrescriptionId
-- points back to it). A real actionable worklist: oldest-signed first.
-- ============================================================================
SELECT TOP (200)
    p.Id AS PrescriptionId,
    p.Scid,
    pat.FirstName + ' ' + pat.LastName AS Patient,
    pr.FirstName + ' ' + pr.LastName AS Provider,
    p.SignedAtUtc,
    DATEDIFF(DAY, p.SignedAtUtc, SYSUTCDATETIME()) AS DaysSinceSigned
FROM Prescription.tblPrescriptions p
JOIN Profile.tblPatients pat ON pat.Id = p.PatientId
JOIN Profile.tblProviders pr ON pr.Id = p.ProviderId
WHERE p.Status = 3 -- Acknowledged
  AND p.SignedAtUtc < DATEADD(DAY, -90, SYSUTCDATETIME())
  AND NOT EXISTS (
      SELECT 1 FROM Prescription.tblPrescriptions repeat_p
      WHERE repeat_p.RepeatOfPrescriptionId = p.Id
  )
ORDER BY p.SignedAtUtc ASC;
