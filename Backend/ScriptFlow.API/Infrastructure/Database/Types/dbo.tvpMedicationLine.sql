-- Table-valued parameter: one prescription medication line, shaped to match
-- ScriptFlow.API.Domain.Entities.PrescriptionMedication exactly. Id is generated in C#
-- (Guid.NewGuid()) before the call, not by the database, so it's always supplied here.
-- Route/Strength/IsPrn/Notes are the optional clinical detail added alongside the original
-- six required fields; Route/Strength/Notes are nullable, IsPrn is a NOT NULL flag.
CREATE TYPE dbo.tvpMedicationLine AS TABLE
(
    Id          UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    MedicineId  UNIQUEIDENTIFIER NOT NULL,
    TakeValue   NVARCHAR(100)    NOT NULL,
    Frequency   NVARCHAR(100)    NOT NULL,
    Duration    NVARCHAR(100)    NOT NULL,
    Quantity    INT              NOT NULL,
    Directions  NVARCHAR(1000)   NOT NULL,
    Route       NVARCHAR(100)    NULL,
    Strength    NVARCHAR(100)    NULL,
    IsPrn       BIT              NOT NULL,
    Notes       NVARCHAR(1000)   NULL,
    Repeats     INT              NOT NULL DEFAULT 0,
    RepeatsUsed INT              NOT NULL DEFAULT 0
);
