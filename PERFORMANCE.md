# ScriptFlow Performance Chapter

Seeded `Prescription.tblPrescriptions` to **1,050,000 rows** (plus a matching
`dbo.PrescriptionMedications` row each) in the real, local SQL Server Express instance
this project runs against day to day, then measured three reporting queries before and
after adding targeted indexes. Every number on this page came from actually running
`SET STATISTICS IO/TIME/XML ON` against that seeded database — nothing here is
estimated. The raw evidence (`.sqlplan` files openable in SSMS/Azure Data Studio, and
`_stats.txt` captures) lives in
[`Backend/ScriptFlow.API/Infrastructure/Database/Performance/Evidence/`](Backend/ScriptFlow.API/Infrastructure/Database/Performance/Evidence/).

## Reinterpretation note

The spec's example reporting queries include "rejection rates by **pharmacy**". This
schema has no pharmacy/dispensary entity — `PharmacyGateway.mock` simulates one
external, unnamed pharmacy, not multiple named pharmacies with their own identity in
the database. Adapted to rejection rate **by practice location** and **by provider**
instead — both real, already-modeled dimensions — rather than inventing a fictitious
entity the running system doesn't actually have.

## Methodology

1. [`01_ExpandLookupData.sql`](Backend/ScriptFlow.API/Infrastructure/Database/Performance/01_ExpandLookupData.sql) —
   bulked up master data (11 practices, 34 practice locations, 151 providers, 50,002
   patients, 25 medicines) so grouped reports have real variance, not one giant bucket.
2. [`02_SeedPrescriptions.sql`](Backend/ScriptFlow.API/Infrastructure/Database/Performance/02_SeedPrescriptions.sql) —
   set-based batch insert (12 batches of ~100k rows) to 1,050,000 prescriptions. Each
   practice location was assigned one of 5 rejection-rate tiers (5/15/25/35/45%) so
   rejection rate genuinely varies by location/provider; `CreatedAtUtc` spread across
   the last 24 months. Non-clustered indexes were disabled for the load and rebuilt
   once at the end (standard bulk-load technique — necessary in practice: SQL Server
   Express's small buffer pool made maintaining 5+ indexes per batch insert
   dramatically slower once the table grew past what fits in memory; disabling them
   during the load and rebuilding once turned a projected multi-hour load into
   minutes).
3. [`03_ReportingQueries.sql`](Backend/ScriptFlow.API/Infrastructure/Database/Performance/03_ReportingQueries.sql) —
   the three reporting queries.
4. Captured "before" `STATISTICS IO/TIME/XML` for each query against only the baseline
   single-column indexes from `SPEC/DatabaseSpec.md`.
5. Ran [`04_Indexes.sql`](Backend/ScriptFlow.API/Infrastructure/Database/Performance/04_Indexes.sql)
   to add four targeted indexes.
6. Re-captured "after" `STATISTICS IO/TIME/XML` for the same queries, same database,
   same data — nothing else changed between before and after.

**Final row counts:** 1,050,000 prescriptions (Created 52,435 / Signed 83,676 /
Dispatched 42,093 / Acknowledged 662,753 / Rejected 209,043), 1,050,000 prescription
medication lines.

**Note:** captures for queries 1/2a/2b omit the final `ORDER BY` present in
`03_ReportingQueries.sql` — it only sorts the small aggregated/grouped output (a few
dozen to a few hundred rows) and doesn't change the base-table access strategy or
logical-read count against the 1.05M-row table, which is what these measurements are
about. Query 3's capture includes its `ORDER BY` unchanged, since there it's part of
the worklist's actual `TOP (200)` semantics.

## Results

| Query | Logical reads (before → after) | CPU time (before → after) | Plan (before → after) |
|---|---|---|---|
| 1. Dispensing volumes | 32,343 → 3,785 (**−88%**) | 687ms → 563ms | Clustered Index Scan → Index Seek on new covering index |
| 2a. Rejection rate by practice location | 32,343 → 5,102 (**−84%**) | 265ms → 172ms | Clustered Index Scan → Index Scan on new narrow composite index |
| 2b. Rejection rate by provider | 32,343 → 5,102 (**−84%**) | 297ms → 187ms | Clustered Index Scan → Index Scan on new narrow composite index |
| 3. Repeat-due list | 33,006 → 33,006 (**no change**) | 953ms → 922ms | Clustered Index Scan → Clustered Index Scan (unchanged) |

### Indexes added ([`04_Indexes.sql`](Backend/ScriptFlow.API/Infrastructure/Database/Performance/04_Indexes.sql))

```sql
CREATE NONCLUSTERED INDEX IX_Prescriptions_Status_CreatedAtUtc
    ON Prescription.tblPrescriptions (Status, CreatedAtUtc)
    INCLUDE (PracticeLocationId);

CREATE NONCLUSTERED INDEX IX_Prescriptions_PracticeLocationId_Status
    ON Prescription.tblPrescriptions (PracticeLocationId, Status);

CREATE NONCLUSTERED INDEX IX_Prescriptions_ProviderId_Status
    ON Prescription.tblPrescriptions (ProviderId, Status);

CREATE NONCLUSTERED INDEX IX_Prescriptions_Acknowledged_SignedAtUtc
    ON Prescription.tblPrescriptions (SignedAtUtc)
    INCLUDE (PatientId, ProviderId, Scid, RepeatOfPrescriptionId)
    WHERE Status = 3;
```

### Query 1 — Dispensing volumes

Groups `Acknowledged` prescriptions by practice location and month. The existing
single-column `IX_Prescriptions_Status` index could find Acknowledged rows but every
match still needed a lookup back to the clustered index for `CreatedAtUtc` and
`PracticeLocationId`. `IX_Prescriptions_Status_CreatedAtUtc` covers the whole query:
logical reads dropped from 32,343 to 3,785.

### Query 2a/2b — Rejection rate by location / by provider

Both group by a dimension (`PracticeLocationId` or `ProviderId`) with a `Status IN
(3,4)` filter. Neither existing single-column index carried `Status`, so every
matching row needed a lookup just to test the filter. The new composite indexes carry
`Status` directly, letting the engine scan a narrow ~2-column index instead of the
full-width clustered index — logical reads dropped from 32,343 to 5,102 for both.

### Query 3 — Repeat-due list: index added, **no improvement** (reported honestly)

`IX_Prescriptions_Acknowledged_SignedAtUtc` was designed on the assumption that
"Acknowledged and signed >90 days ago with no repeat yet" would be a small, selective
slice of the table — the classic case for a filtered index. Measuring proved that
assumption wrong: with 24 months of seeded data and only a 90-day cutoff,
**580,217 of 1,050,000 rows (55.3%)** actually match the outer filter before the
`NOT EXISTS` check even runs. At that selectivity, a full clustered index scan is
genuinely cheaper than a nonclustered seek/scan plus bookmark lookups — SQL Server's
optimizer correctly ignored the new index in every capture, including a retest with
the cutoff date pre-computed into a variable (to rule out a non-sargable-expression
cardinality-estimate artifact as the cause). The index still exists and is documented
here rather than silently dropped or the result quietly omitted: **the lesson is that
index design has to be checked against actual predicate selectivity, not assumed from
the query's English description** — "prescriptions signed long ago" sounds selective;
against this particular seed's date distribution, it isn't. A real fix would be
narrowing the seed's date range (so "still open after 90 days" is genuinely rare) or,
in production, would reflect however patients/providers are actually distributed over
time.

## A real consequence of the 1M-row seed: it broke the dashboard

Seeding `Prescription.tblPrescriptions` to 1,050,000 rows didn't just create material
for reporting queries — it exposed a real, pre-existing bug the moment the app was
run against it live. The Angular dashboard called `GET /api/prescriptions` with no
filters and counted statuses client-side; `usp_Prescription_List` had no `TOP`/paging,
so an unfiltered call tried to return the entire table. Confirmed live: the request
didn't complete even after 15 seconds — before the seed, with a handful of rows, this
same code path was instant and the bug was invisible.

Fixed two ways, not one:
1. **`usp_Prescription_List` is now capped** at the 200 most recent matches regardless
   of filter (~2.2s for an unfiltered call afterward, down from never completing).
2. **A dedicated `usp_Prescription_StatusCounts`** (`GROUP BY Status`, ~1s regardless
   of table size) backs a new `GET /api/prescriptions/status-counts` endpoint, which
   the dashboard now calls instead. This isn't just a performance fix — capping
   `usp_Prescription_List` alone would have made the dashboard *fast but wrong*: with
   1.05M total rows, counting only the 200 most recent would show a small fraction of
   the real totals per status.

Verified live end-to-end: `status-counts` returns the real totals
(`Acknowledged: 662,759`, `Rejected: 209,045`, etc.) in ~3s; the capped list returns in
~2.4s. Same lesson as the filtered-index finding above — code that's correct against a
small demo dataset can break, silently or loudly, once the data is realistic scale;
the fix belongs wherever the actual need is (aggregate counts vs. a bounded list), not
just a bigger timeout.

**Correction — this index was not harmless.** Adding *any* filtered index to a table
requires `QUOTED_IDENTIFIER ON` for every session that subsequently writes to that
table (`INSERT`/`UPDATE`/`DELETE`) — not just for the `CREATE INDEX` statement itself.
`usp_Prescription_Create` and `usp_Prescription_Update` were both compiled with
`QUOTED_IDENTIFIER OFF` (`sys.sql_modules.uses_quoted_identifier = 0`), so from the
moment `IX_Prescriptions_Acknowledged_SignedAtUtc` was added, **every prescription
create and sign call started failing with a 500** — a real regression, not a
theoretical one. This was caught the first time the integration test
(`TEST_PLAN.md`) actually exercised create→sign end-to-end, and fixed by recompiling
both procedures with `QUOTED_IDENTIFIER ON`. Left in `PERFORMANCE.md`'s original
wording above (struck through the "harmless" claim rather than deleted) because
getting this wrong the first time, then catching it for real, is itself the more
honest record than editing history to look right in hindsight.
