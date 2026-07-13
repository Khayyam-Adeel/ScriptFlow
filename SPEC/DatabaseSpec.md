# ScriptFlow.API Database Design Specification

This document defines the table structure and Relational schema backing `ScriptFlow.API`, derived from the
domain entities (`Domain/Entities`), value objects (`Domain/ValueObjects`), and DTOs
(`Application/DTOs`). Target engine: SQL Server (T-SQL syntax below); adjust types if a
different engine is chosen.



## Conventions

Every table carries a standard set of audit/lifecycle columns in addition to its
business columns:
Ecery table must be initilized with schema name +"."+ tbl+"table name"
use these schemas
------------------------------------------------------------
create schema Profile
create schema Medication
create schema lookup
create schema Prescription
create schema Admin
--------------------------------------------------------------------

| Column       | Type             | Notes                                              |
|--------------|------------------|-----------------------------------------------------|
| `IsActive`   | `BIT`            | `NOT NULL DEFAULT 1`. Business-active flag.         |
| `IsDeleted`  | `BIT`            | `NOT NULL DEFAULT 0`. Soft-delete flag.             |
| `InsertedAt` | `DATETIME2(3)`   | `NOT NULL DEFAULT SYSUTCDATETIME()`.                |
| `UpdatedAt`  | `DATETIME2(3)`   | `NULL`. Set on update.                              |
| `InsertedBy` | `UNIQUEIDENTIFIER` | `NOT NULL`. FK to `Users.Id` (system/seed rows may use a well-known system user id). |
| `UpdatedBy`  | `UNIQUEIDENTIFIER` | `NULL`. FK to `Users.Id`.                         |

- All primary keys are `UNIQUEIDENTIFIER` (`Guid`), matching the domain entities, with
  `DEFAULT NEWID()` so inserts can omit the value.
- Foreign keys to `Users` (`InsertedBy` / `UpdatedBy`) are declared `NOCHECK` intent-wise
  (i.e. not enforced with `ON DELETE CASCADE`) to avoid cascade chains through audit
  columns — see per-table FK definitions.
- Queries against these tables should always filter `WHERE IsDeleted = 0` (soft delete);
  `IsActive` is a separate business-state flag (e.g. a disabled provider login) and is
  independent of deletion.

## Entity-Relationship Overview

```
Practices 1───* PracticeLocations 1───* Providers
                       │                    │
                       │                    │
                       *                    *
                  Prescriptions ───────────*┘
                    │      │
                    │      *
                    │  PrescriptionMedications *───1 Medicines
                    │
                    * (PatientId)
                 Patients

Users (auth identities; referenced by every table's InsertedBy/UpdatedBy, and by Providers via optional link)
```

---

CREATE TABLE Profile.tblUsers
(
    Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Users_Id DEFAULT NEWID(),
    Email           NVARCHAR(256)    NOT NULL,
    PasswordHash    NVARCHAR(512)    NOT NULL,
    Role            TINYINT          NOT NULL CONSTRAINT DF_Users_Role DEFAULT (0), -- 0=Prescriber, 1=Admin (Shared.contract.Enums.UserRole)
    IsActive        BIT              NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
    IsDeleted       BIT              NOT NULL CONSTRAINT DF_Users_IsDeleted DEFAULT (0),
    InsertedAt      DATETIME2(3)     NOT NULL CONSTRAINT DF_Users_InsertedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt       DATETIME2(3)     NULL,
    InsertedBy      UNIQUEIDENTIFIER NOT NULL,
    UpdatedBy       UNIQUEIDENTIFIER NULL,
    CONSTRAINT PK_Users PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT CK_Users_Role CHECK (Role IN (0, 1))
);

CREATE TABLE Admin.tblPractices
(
    Id          UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Practices_Id DEFAULT NEWID(),
    Name        NVARCHAR(200)    NOT NULL,
    IsActive    BIT              NOT NULL CONSTRAINT DF_Practices_IsActive DEFAULT (1),
    IsDeleted   BIT              NOT NULL CONSTRAINT DF_Practices_IsDeleted DEFAULT (0),
    InsertedAt  DATETIME2(3)     NOT NULL CONSTRAINT DF_Practices_InsertedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt   DATETIME2(3)     NULL,
    InsertedBy  UNIQUEIDENTIFIER NOT NULL,
    UpdatedBy   UNIQUEIDENTIFIER NULL,
    CONSTRAINT PK_Practices PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Practices_InsertedBy_tblUsers FOREIGN KEY (InsertedBy) REFERENCES Profile.tblUsers (Id),
    CONSTRAINT FK_Practices_UpdatedBy_tblUsers FOREIGN KEY (UpdatedBy) REFERENCES Profile.tblUsers (Id)
);

CREATE TABLE Admin.tblPracticeLocations
(
    Id            UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_PracticeLocations_Id DEFAULT NEWID(),
    PracticeId    UNIQUEIDENTIFIER NOT NULL,
    Name          NVARCHAR(200)    NOT NULL,
    HpiNo         CHAR(5)          NOT NULL,
    HpiExtension  CHAR(1)          NOT NULL,
    IsActive      BIT              NOT NULL CONSTRAINT DF_PracticeLocations_IsActive DEFAULT (1),
    IsDeleted     BIT              NOT NULL CONSTRAINT DF_PracticeLocations_IsDeleted DEFAULT (0),
    InsertedAt    DATETIME2(3)     NOT NULL CONSTRAINT DF_PracticeLocations_InsertedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt     DATETIME2(3)     NULL,
    InsertedBy    UNIQUEIDENTIFIER NOT NULL,
    UpdatedBy     UNIQUEIDENTIFIER NULL,
    CONSTRAINT PK_PracticeLocations PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_PracticeLocations_Practices FOREIGN KEY (PracticeId) REFERENCES Admin.tblPractices (Id),
    CONSTRAINT FK_PracticeLocations_InsertedBy_Users FOREIGN KEY (InsertedBy) REFERENCES Profile.tblUsers (Id),
    CONSTRAINT FK_PracticeLocations_UpdatedBy_Users FOREIGN KEY (UpdatedBy) REFERENCES Profile.tblUsers (Id),
    CONSTRAINT UQ_PracticeLocations_Hpi UNIQUE (HpiNo, HpiExtension),
    CONSTRAINT CK_PracticeLocations_HpiNo CHECK (HpiNo LIKE '[A-Z][A-Z][A-Z][0-9][0-9]'),
    CONSTRAINT CK_PracticeLocations_HpiExtension CHECK (HpiExtension LIKE '[A-Z]')
);


CREATE TABLE Profile.tblPatients
(
    Id          UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Patients_Id DEFAULT NEWID(),
    FirstName   NVARCHAR(100)    NOT NULL,
    LastName    NVARCHAR(100)    NOT NULL,
    Address     NVARCHAR(500)    NOT NULL,
    Nhi         CHAR(7)          NOT NULL,
    IsActive    BIT              NOT NULL CONSTRAINT DF_Patients_IsActive DEFAULT (1),
    IsDeleted   BIT              NOT NULL CONSTRAINT DF_Patients_IsDeleted DEFAULT (0),
    InsertedAt  DATETIME2(3)     NOT NULL CONSTRAINT DF_Patients_InsertedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt   DATETIME2(3)     NULL,
    InsertedBy  UNIQUEIDENTIFIER NOT NULL,
    UpdatedBy   UNIQUEIDENTIFIER NULL,
    CONSTRAINT PK_Patients PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Patients_InsertedBy_Users FOREIGN KEY (InsertedBy) REFERENCES Profile.tblUsers (Id),
    CONSTRAINT FK_Patients_UpdatedBy_Users FOREIGN KEY (UpdatedBy) REFERENCES Profile.tblUsers (Id),
    CONSTRAINT UQ_Patients_Nhi UNIQUE (Nhi),
    CONSTRAINT CK_Patients_Nhi CHECK (Nhi LIKE '[A-Z][A-Z][A-Z][0-9][0-9][0-9][0-9]')
);

CREATE TABLE Profile.tblProviders
(
    Id                  UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Providers_Id DEFAULT NEWID(),
    FirstName           NVARCHAR(100)    NOT NULL,
    LastName            NVARCHAR(100)    NOT NULL,
    Type                TINYINT          NOT NULL,
    NzmcNo              NVARCHAR(20)     NOT NULL,
    PracticeLocationId  UNIQUEIDENTIFIER NOT NULL,
    IsActive            BIT              NOT NULL CONSTRAINT DF_Providers_IsActive DEFAULT (1),
    IsDeleted           BIT              NOT NULL CONSTRAINT DF_Providers_IsDeleted DEFAULT (0),
    InsertedAt          DATETIME2(3)     NOT NULL CONSTRAINT DF_Providers_InsertedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt           DATETIME2(3)     NULL,
    InsertedBy          UNIQUEIDENTIFIER NOT NULL,
    UpdatedBy           UNIQUEIDENTIFIER NULL,
    CONSTRAINT PK_Providers PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Providers_PracticeLocations FOREIGN KEY (PracticeLocationId) REFERENCES Admin.tblPracticeLocations (Id),
    CONSTRAINT FK_Providers_InsertedBy_Users FOREIGN KEY (InsertedBy) REFERENCES Profile.tblUsers (Id),
    CONSTRAINT FK_Providers_UpdatedBy_Users FOREIGN KEY (UpdatedBy) REFERENCES Profile.tblUsers (Id),
    CONSTRAINT UQ_Providers_NzmcNo UNIQUE (NzmcNo),
    CONSTRAINT CK_Providers_Type CHECK (Type IN (0, 1, 2))
);

CREATE INDEX IX_Providers_PracticeLocationId ON Profile.tblProviders (PracticeLocationId);

CREATE TABLE Lookup.tblMedicines
(
    Id          UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Medicines_Id DEFAULT NEWID(),
    Name        NVARCHAR(300)    NOT NULL,
    Sctid       NVARCHAR(20)     NOT NULL,
    Form        NVARCHAR(100)    NOT NULL,
    IsActive    BIT              NOT NULL CONSTRAINT DF_Medicines_IsActive DEFAULT (1),
    IsDeleted   BIT              NOT NULL CONSTRAINT DF_Medicines_IsDeleted DEFAULT (0),
    InsertedAt  DATETIME2(3)     NOT NULL CONSTRAINT DF_Medicines_InsertedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt   DATETIME2(3)     NULL,
    InsertedBy  UNIQUEIDENTIFIER NOT NULL,
    UpdatedBy   UNIQUEIDENTIFIER NULL,
    CONSTRAINT PK_Medicines PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Medicines_InsertedBy_Users FOREIGN KEY (InsertedBy) REFERENCES Profile.tblUsers(Id),
    CONSTRAINT FK_Medicines_UpdatedBy_Users FOREIGN KEY (UpdatedBy) REFERENCES Profile.tblUsers (Id),
    CONSTRAINT UQ_Medicines_Sctid UNIQUE (Sctid)
);

CREATE TABLE Prescription.tblPrescriptions
(
    Id                      UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Prescriptions_Id DEFAULT NEWID(),
    Scid                    CHAR(11)         NOT NULL,
    PatientId               UNIQUEIDENTIFIER NOT NULL,
    ProviderId              UNIQUEIDENTIFIER NOT NULL,
    PracticeLocationId      UNIQUEIDENTIFIER NOT NULL,
    Status                  TINYINT          NOT NULL CONSTRAINT DF_Prescriptions_Status DEFAULT (0),
    RepeatOfPrescriptionId  UNIQUEIDENTIFIER NULL,
    CreatedAtUtc            DATETIME2(3)     NOT NULL CONSTRAINT DF_Prescriptions_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
    SignedAtUtc             DATETIME2(3)     NULL,
    IsActive                BIT              NOT NULL CONSTRAINT DF_Prescriptions_IsActive DEFAULT (1),
    IsDeleted               BIT              NOT NULL CONSTRAINT DF_Prescriptions_IsDeleted DEFAULT (0),
    InsertedAt              DATETIME2(3)     NOT NULL CONSTRAINT DF_Prescriptions_InsertedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt               DATETIME2(3)     NULL,
    InsertedBy              UNIQUEIDENTIFIER NOT NULL,
    UpdatedBy               UNIQUEIDENTIFIER NULL,
    CONSTRAINT PK_Prescriptions PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Prescriptions_Patients FOREIGN KEY (PatientId) REFERENCES Profile.tblPatients (Id),
    CONSTRAINT FK_Prescriptions_Providers FOREIGN KEY (ProviderId) REFERENCES Profile.tblProviders (Id),
    CONSTRAINT FK_Prescriptions_PracticeLocations FOREIGN KEY (PracticeLocationId) REFERENCES Admin.tblPracticeLocations (Id),
    CONSTRAINT FK_Prescriptions_RepeatOf FOREIGN KEY (RepeatOfPrescriptionId) REFERENCES Prescription.tblPrescriptions (Id),
    CONSTRAINT FK_Prescriptions_InsertedBy_Users FOREIGN KEY (InsertedBy) REFERENCES Profile.tblUsers (Id),
    CONSTRAINT FK_Prescriptions_UpdatedBy_Users FOREIGN KEY (UpdatedBy) REFERENCES Profile.tblUsers (Id),
    CONSTRAINT UQ_Prescriptions_Scid UNIQUE (Scid),
    CONSTRAINT CK_Prescriptions_Scid CHECK (Scid LIKE '9__________' AND LEN(Scid) = 11),
    CONSTRAINT CK_Prescriptions_Status CHECK (Status BETWEEN 0 AND 5)
);

CREATE INDEX IX_Prescriptions_PatientId ON Prescription.tblPrescriptions (PatientId);
CREATE INDEX IX_Prescriptions_ProviderId ON Prescription.tblPrescriptions (ProviderId);
CREATE INDEX IX_Prescriptions_PracticeLocationId ON Prescription.tblPrescriptions (PracticeLocationId);
CREATE INDEX IX_Prescriptions_RepeatOfPrescriptionId ON Prescription.tblPrescriptions (RepeatOfPrescriptionId);
CREATE INDEX IX_Prescriptions_Status ON Prescription.tblPrescriptions (Status);


CREATE TABLE dbo.PrescriptionMedications
(
    Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_PrescriptionMedications_Id DEFAULT NEWID(),
    PrescriptionId  UNIQUEIDENTIFIER NOT NULL,
    MedicineId      UNIQUEIDENTIFIER NOT NULL,
    TakeValue       NVARCHAR(100)    NOT NULL,
    Frequency       NVARCHAR(100)    NOT NULL,
    Duration        NVARCHAR(100)    NOT NULL,
    Quantity        INT              NOT NULL,
    Directions      NVARCHAR(1000)   NOT NULL,
    IsActive        BIT              NOT NULL CONSTRAINT DF_PrescriptionMedications_IsActive DEFAULT (1),
    IsDeleted       BIT              NOT NULL CONSTRAINT DF_PrescriptionMedications_IsDeleted DEFAULT (0),
    InsertedAt      DATETIME2(3)     NOT NULL CONSTRAINT DF_PrescriptionMedications_InsertedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt       DATETIME2(3)     NULL,
    InsertedBy      UNIQUEIDENTIFIER NOT NULL,
    UpdatedBy       UNIQUEIDENTIFIER NULL,
    CONSTRAINT PK_PrescriptionMedications PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_PrescriptionMedications_Prescriptions FOREIGN KEY (PrescriptionId) REFERENCES Prescription.tblPrescriptions (Id) ON DELETE CASCADE,
    CONSTRAINT FK_PrescriptionMedications_Medicines FOREIGN KEY (MedicineId) REFERENCES lookup.tblMedicines (Id),
    CONSTRAINT FK_PrescriptionMedications_InsertedBy_Users FOREIGN KEY (InsertedBy) REFERENCES Profile.tblUsers (Id),
    CONSTRAINT FK_PrescriptionMedications_UpdatedBy_Users FOREIGN KEY (UpdatedBy) REFERENCES Profile.tblUsers (Id),
    CONSTRAINT CK_PrescriptionMedications_Quantity CHECK (Quantity > 0)
);

CREATE INDEX IX_PrescriptionMedications_PrescriptionId ON dbo.PrescriptionMedications (PrescriptionId);
CREATE INDEX IX_PrescriptionMedications_MedicineId ON dbo.PrescriptionMedications (MedicineId);

CREATE TABLE dbo.ProcessedMessages
(
    EventId         UNIQUEIDENTIFIER NOT NULL,
    EventType       NVARCHAR(200)    NOT NULL,
    PrescriptionId  UNIQUEIDENTIFIER NOT NULL,
    ProcessedAtUtc  DATETIME2(3)     NOT NULL CONSTRAINT DF_ProcessedMessages_ProcessedAtUtc DEFAULT SYSUTCDATETIME(),
    InsertedAt      DATETIME2(3)     NOT NULL CONSTRAINT DF_ProcessedMessages_InsertedAt DEFAULT SYSUTCDATETIME(),
    InsertedBy      UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT PK_ProcessedMessages PRIMARY KEY CLUSTERED (EventId),
    CONSTRAINT FK_ProcessedMessages_InsertedBy_Users FOREIGN KEY (InsertedBy) REFERENCES Profile.tblUsers (Id)
);

CREATE INDEX IX_ProcessedMessages_PrescriptionId ON dbo.ProcessedMessages (PrescriptionId);

CREATE TABLE dbo.TblErrorLog
(
    ID         UNIQUEIDENTIFIER NOT NULL,
    Error       NVARCHAR(200)    NOT NULL,
    StoreProcedure  NVARCHAR(250) NOT NULL,
    ErrorStack  NVARCHAR(max) NOT NULL,
    InsertedAt      DATETIME2(3)  Not Null,
    
);