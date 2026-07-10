-- Table-valued parameter: one prescription medication line, shaped to match
-- ScriptFlow.API.Domain.Entities.PrescriptionMedication exactly. Id is generated in C#
-- (Guid.NewGuid()) before the call, not by the database, so it's always supplied here.
CREATE TYPE dbo.tvpMedicationLine AS TABLE
(
    Id          UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    MedicineId  UNIQUEIDENTIFIER NOT NULL,
    TakeValue   NVARCHAR(100)    NOT NULL,
    Frequency   NVARCHAR(100)    NOT NULL,
    Duration    NVARCHAR(100)    NOT NULL,
    Quantity    INT              NOT NULL,
    Directions  NVARCHAR(1000)   NOT NULL
);
