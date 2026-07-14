-- Full schema (schemas, table types, tables, indexes, defaults, foreign keys, check
-- constraints) for a brand-new ScriptFlow database - extracted via `sqlpackage /Action:Extract`
-- + `/Action:Script` against the real dev database (dbserver-local/ScriptFlow_DEV), then hand-
-- trimmed to schema-only: stored procedures are deliberately NOT included here, even though the
-- raw extraction captured them too - they already have one canonical source of truth per file
-- under ../StoredProcedures/ (edited/reviewed individually, CREATE OR ALTER), and duplicating
-- their bodies into this file would create a second, driftable copy of the same objects.
--
-- Run this FIRST (creates every table StoredProcedures/*.sql and Types/*.sql assume already
-- exist), then deploy Types/*.sql and StoredProcedures/*.sql on top, then optionally
-- Performance/01_ExpandLookupData.sql to get usable seed data. See docker-compose.yml's
-- db-init service for the exact order this project runs these in for a one-command startup.
--
-- Idempotent-ish: safe to run against a database that doesn't have these objects yet (a fresh
-- container). NOT safe to re-run against a database that already has them - unlike the
-- CREATE OR ALTER stored procedures, none of this uses IF NOT EXISTS guards, matching how
-- sqlpackage itself generates a create (not upsert) script.
--
-- Requires SQLCMD mode (plain sqlcmd has this on by default; SSMS needs Query > SQLCMD Mode).
:setvar DatabaseName "ScriptFlow"
GO

-- No ON/LOG ON clause deliberately - omitting it lets SQL Server put the files in whatever
-- its own configured default data/log path is, which is the only thing that stays portable
-- across a Linux Docker container, a GitHub Actions service container, and a real Windows SQL
-- Server instance without hardcoding any of their differing default paths.
IF DB_ID(N'$(DatabaseName)') IS NULL
BEGIN
    PRINT N'Creating database $(DatabaseName)...';
    CREATE DATABASE [$(DatabaseName)];
END
GO
USE [$(DatabaseName)];
GO

-- QUOTED_IDENTIFIER ON is required for the filtered indexes below (IX_Prescriptions_
-- Acknowledged_SignedAtUtc/CreatedAtUtc/Scid) - same requirement already documented in
-- Performance/05_FixQuotedIdentifierForFilteredIndex.sql for the stored procedures that write
-- to these tables. Matches the SET options sqlpackage itself generated for this script.
SET ANSI_NULLS, ANSI_PADDING, ANSI_WARNINGS, ARITHABORT, CONCAT_NULL_YIELDS_NULL, QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
GO

PRINT N'Creating Schema [Admin]...';


GO
CREATE SCHEMA [Admin]
    AUTHORIZATION [dbo];


GO
PRINT N'Creating Schema [Lookup]...';


GO
CREATE SCHEMA [Lookup]
    AUTHORIZATION [dbo];


GO
PRINT N'Creating Schema [Medication]...';


GO
CREATE SCHEMA [Medication]
    AUTHORIZATION [dbo];


GO
PRINT N'Creating Schema [Prescription]...';


GO
CREATE SCHEMA [Prescription]
    AUTHORIZATION [dbo];


GO
PRINT N'Creating Schema [Profile]...';


GO
CREATE SCHEMA [Profile]
    AUTHORIZATION [dbo];


GO
PRINT N'Creating User-Defined Table Type [dbo].[tvpGuidList]...';


GO
CREATE TYPE [dbo].[tvpGuidList] AS TABLE (
    [Id] UNIQUEIDENTIFIER NOT NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC));


GO
PRINT N'Creating User-Defined Table Type [dbo].[tvpMedicationLine]...';


GO
CREATE TYPE [dbo].[tvpMedicationLine] AS TABLE (
    [Id]         UNIQUEIDENTIFIER NOT NULL,
    [MedicineId] UNIQUEIDENTIFIER NOT NULL,
    [TakeValue]  NVARCHAR (100)   NOT NULL,
    [Frequency]  NVARCHAR (100)   NOT NULL,
    [Duration]   NVARCHAR (100)   NOT NULL,
    [Quantity]   INT              NOT NULL,
    [Directions] NVARCHAR (1000)  NOT NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC));


GO
PRINT N'Creating Table [Admin].[tblPracticeLocations]...';


GO
CREATE TABLE [Admin].[tblPracticeLocations] (
    [Id]           UNIQUEIDENTIFIER NOT NULL,
    [PracticeId]   UNIQUEIDENTIFIER NOT NULL,
    [Name]         NVARCHAR (200)   NOT NULL,
    [HpiNo]        CHAR (5)         NOT NULL,
    [HpiExtension] CHAR (1)         NOT NULL,
    [IsActive]     BIT              NOT NULL,
    [IsDeleted]    BIT              NOT NULL,
    [InsertedAt]   DATETIME2 (3)    NOT NULL,
    [UpdatedAt]    DATETIME2 (3)    NULL,
    [InsertedBy]   UNIQUEIDENTIFIER NOT NULL,
    [UpdatedBy]    UNIQUEIDENTIFIER NULL,
    CONSTRAINT [PK_PracticeLocations] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_PracticeLocations_Hpi] UNIQUE NONCLUSTERED ([HpiNo] ASC, [HpiExtension] ASC)
);


GO
PRINT N'Creating Table [Admin].[tblPractices]...';


GO
CREATE TABLE [Admin].[tblPractices] (
    [Id]         UNIQUEIDENTIFIER NOT NULL,
    [Name]       NVARCHAR (200)   NOT NULL,
    [IsActive]   BIT              NOT NULL,
    [IsDeleted]  BIT              NOT NULL,
    [InsertedAt] DATETIME2 (3)    NOT NULL,
    [UpdatedAt]  DATETIME2 (3)    NULL,
    [InsertedBy] UNIQUEIDENTIFIER NOT NULL,
    [UpdatedBy]  UNIQUEIDENTIFIER NULL,
    CONSTRAINT [PK_Practices] PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
PRINT N'Creating Table [Lookup].[tblMedicines]...';


GO
CREATE TABLE [Lookup].[tblMedicines] (
    [Id]         UNIQUEIDENTIFIER NOT NULL,
    [Name]       NVARCHAR (300)   NOT NULL,
    [Sctid]      NVARCHAR (20)    NOT NULL,
    [Form]       NVARCHAR (100)   NOT NULL,
    [IsActive]   BIT              NOT NULL,
    [IsDeleted]  BIT              NOT NULL,
    [InsertedAt] DATETIME2 (3)    NOT NULL,
    [UpdatedAt]  DATETIME2 (3)    NULL,
    [InsertedBy] UNIQUEIDENTIFIER NOT NULL,
    [UpdatedBy]  UNIQUEIDENTIFIER NULL,
    CONSTRAINT [PK_Medicines] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_Medicines_Sctid] UNIQUE NONCLUSTERED ([Sctid] ASC)
);


GO
PRINT N'Creating Table [Prescription].[tblPrescriptions]...';


GO
CREATE TABLE [Prescription].[tblPrescriptions] (
    [Id]                     UNIQUEIDENTIFIER NOT NULL,
    [Scid]                   CHAR (11)        NOT NULL,
    [PatientId]              UNIQUEIDENTIFIER NOT NULL,
    [ProviderId]             UNIQUEIDENTIFIER NOT NULL,
    [PracticeLocationId]     UNIQUEIDENTIFIER NOT NULL,
    [Status]                 TINYINT          NOT NULL,
    [RepeatOfPrescriptionId] UNIQUEIDENTIFIER NULL,
    [CreatedAtUtc]           DATETIME2 (3)    NOT NULL,
    [SignedAtUtc]            DATETIME2 (3)    NULL,
    [IsActive]               BIT              NOT NULL,
    [IsDeleted]              BIT              NOT NULL,
    [InsertedAt]             DATETIME2 (3)    NOT NULL,
    [UpdatedAt]              DATETIME2 (3)    NULL,
    [InsertedBy]             UNIQUEIDENTIFIER NOT NULL,
    [UpdatedBy]              UNIQUEIDENTIFIER NULL,
    CONSTRAINT [PK_Prescriptions] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_Prescriptions_Scid] UNIQUE NONCLUSTERED ([Scid] ASC)
);


GO
PRINT N'Creating Index [Prescription].[tblPrescriptions].[IX_Prescriptions_ProviderId]...';


GO
CREATE NONCLUSTERED INDEX [IX_Prescriptions_ProviderId]
    ON [Prescription].[tblPrescriptions]([ProviderId] ASC);


GO
PRINT N'Creating Index [Prescription].[tblPrescriptions].[IX_Prescriptions_Acknowledged_SignedAtUtc]...';


GO
CREATE NONCLUSTERED INDEX [IX_Prescriptions_Acknowledged_SignedAtUtc]
    ON [Prescription].[tblPrescriptions]([SignedAtUtc] ASC)
    INCLUDE([PatientId], [ProviderId], [Scid], [RepeatOfPrescriptionId]) WHERE ([Status]=(3));


GO
PRINT N'Creating Index [Prescription].[tblPrescriptions].[IX_Prescriptions_CreatedAtUtc]...';


GO
CREATE NONCLUSTERED INDEX [IX_Prescriptions_CreatedAtUtc]
    ON [Prescription].[tblPrescriptions]([CreatedAtUtc] DESC)
    INCLUDE([Scid], [PatientId], [ProviderId], [PracticeLocationId], [Status], [RepeatOfPrescriptionId], [SignedAtUtc]) WHERE ([IsDeleted]=(0));


GO
PRINT N'Creating Index [Prescription].[tblPrescriptions].[IX_Prescriptions_Status]...';


GO
CREATE NONCLUSTERED INDEX [IX_Prescriptions_Status]
    ON [Prescription].[tblPrescriptions]([Status] ASC);


GO
PRINT N'Creating Index [Prescription].[tblPrescriptions].[IX_Prescriptions_RepeatOfPrescriptionId]...';


GO
CREATE NONCLUSTERED INDEX [IX_Prescriptions_RepeatOfPrescriptionId]
    ON [Prescription].[tblPrescriptions]([RepeatOfPrescriptionId] ASC);


GO
PRINT N'Creating Index [Prescription].[tblPrescriptions].[IX_Prescriptions_PatientId]...';


GO
CREATE NONCLUSTERED INDEX [IX_Prescriptions_PatientId]
    ON [Prescription].[tblPrescriptions]([PatientId] ASC);


GO
PRINT N'Creating Index [Prescription].[tblPrescriptions].[IX_Prescriptions_PracticeLocationId_Status]...';


GO
CREATE NONCLUSTERED INDEX [IX_Prescriptions_PracticeLocationId_Status]
    ON [Prescription].[tblPrescriptions]([PracticeLocationId] ASC, [Status] ASC);


GO
PRINT N'Creating Index [Prescription].[tblPrescriptions].[IX_Prescriptions_Scid]...';


GO
CREATE NONCLUSTERED INDEX [IX_Prescriptions_Scid]
    ON [Prescription].[tblPrescriptions]([Scid] ASC)
    INCLUDE([PatientId], [ProviderId], [PracticeLocationId], [Status], [RepeatOfPrescriptionId], [CreatedAtUtc], [SignedAtUtc]) WHERE ([IsDeleted]=(0));


GO
PRINT N'Creating Index [Prescription].[tblPrescriptions].[IX_Prescriptions_Status_CreatedAtUtc]...';


GO
CREATE NONCLUSTERED INDEX [IX_Prescriptions_Status_CreatedAtUtc]
    ON [Prescription].[tblPrescriptions]([Status] ASC, [CreatedAtUtc] ASC)
    INCLUDE([PracticeLocationId]);


GO
PRINT N'Creating Index [Prescription].[tblPrescriptions].[IX_Prescriptions_ProviderId_Status]...';


GO
CREATE NONCLUSTERED INDEX [IX_Prescriptions_ProviderId_Status]
    ON [Prescription].[tblPrescriptions]([ProviderId] ASC, [Status] ASC);


GO
PRINT N'Creating Index [Prescription].[tblPrescriptions].[IX_Prescriptions_PracticeLocationId]...';


GO
CREATE NONCLUSTERED INDEX [IX_Prescriptions_PracticeLocationId]
    ON [Prescription].[tblPrescriptions]([PracticeLocationId] ASC);


GO
PRINT N'Creating Table [Profile].[tblProviders]...';


GO
CREATE TABLE [Profile].[tblProviders] (
    [Id]                 UNIQUEIDENTIFIER NOT NULL,
    [FirstName]          NVARCHAR (100)   NOT NULL,
    [LastName]           NVARCHAR (100)   NOT NULL,
    [Type]               TINYINT          NOT NULL,
    [NzmcNo]             NVARCHAR (20)    NOT NULL,
    [PracticeLocationId] UNIQUEIDENTIFIER NOT NULL,
    [IsActive]           BIT              NOT NULL,
    [IsDeleted]          BIT              NOT NULL,
    [InsertedAt]         DATETIME2 (3)    NOT NULL,
    [UpdatedAt]          DATETIME2 (3)    NULL,
    [InsertedBy]         UNIQUEIDENTIFIER NOT NULL,
    [UpdatedBy]          UNIQUEIDENTIFIER NULL,
    CONSTRAINT [PK_Providers] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_Providers_NzmcNo] UNIQUE NONCLUSTERED ([NzmcNo] ASC)
);


GO
PRINT N'Creating Index [Profile].[tblProviders].[IX_Providers_PracticeLocationId]...';


GO
CREATE NONCLUSTERED INDEX [IX_Providers_PracticeLocationId]
    ON [Profile].[tblProviders]([PracticeLocationId] ASC);


GO
PRINT N'Creating Table [Profile].[tblPatients]...';


GO
CREATE TABLE [Profile].[tblPatients] (
    [Id]         UNIQUEIDENTIFIER NOT NULL,
    [FirstName]  NVARCHAR (100)   NOT NULL,
    [LastName]   NVARCHAR (100)   NOT NULL,
    [Address]    NVARCHAR (500)   NOT NULL,
    [Nhi]        CHAR (7)         NOT NULL,
    [IsActive]   BIT              NOT NULL,
    [IsDeleted]  BIT              NOT NULL,
    [InsertedAt] DATETIME2 (3)    NOT NULL,
    [UpdatedAt]  DATETIME2 (3)    NULL,
    [InsertedBy] UNIQUEIDENTIFIER NOT NULL,
    [UpdatedBy]  UNIQUEIDENTIFIER NULL,
    CONSTRAINT [PK_Patients] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_Patients_Nhi] UNIQUE NONCLUSTERED ([Nhi] ASC)
);


GO
PRINT N'Creating Table [Profile].[tblUsers]...';


GO
CREATE TABLE [Profile].[tblUsers] (
    [Id]           UNIQUEIDENTIFIER NOT NULL,
    [Email]        NVARCHAR (256)   NOT NULL,
    [PasswordHash] NVARCHAR (512)   NOT NULL,
    [IsActive]     BIT              NOT NULL,
    [IsDeleted]    BIT              NOT NULL,
    [InsertedAt]   DATETIME2 (3)    NOT NULL,
    [UpdatedAt]    DATETIME2 (3)    NULL,
    [InsertedBy]   UNIQUEIDENTIFIER NOT NULL,
    [UpdatedBy]    UNIQUEIDENTIFIER NULL,
    [Role]         TINYINT          NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_Users_Email] UNIQUE NONCLUSTERED ([Email] ASC)
);


GO
PRINT N'Creating Table [dbo].[PrescriptionMedications]...';


GO
CREATE TABLE [dbo].[PrescriptionMedications] (
    [Id]             UNIQUEIDENTIFIER NOT NULL,
    [PrescriptionId] UNIQUEIDENTIFIER NOT NULL,
    [MedicineId]     UNIQUEIDENTIFIER NOT NULL,
    [TakeValue]      NVARCHAR (100)   NOT NULL,
    [Frequency]      NVARCHAR (100)   NOT NULL,
    [Duration]       NVARCHAR (100)   NOT NULL,
    [Quantity]       INT              NOT NULL,
    [Directions]     NVARCHAR (1000)  NOT NULL,
    [IsActive]       BIT              NOT NULL,
    [IsDeleted]      BIT              NOT NULL,
    [InsertedAt]     DATETIME2 (3)    NOT NULL,
    [UpdatedAt]      DATETIME2 (3)    NULL,
    [InsertedBy]     UNIQUEIDENTIFIER NOT NULL,
    [UpdatedBy]      UNIQUEIDENTIFIER NULL,
    CONSTRAINT [PK_PrescriptionMedications] PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
PRINT N'Creating Index [dbo].[PrescriptionMedications].[IX_PrescriptionMedications_MedicineId]...';


GO
CREATE NONCLUSTERED INDEX [IX_PrescriptionMedications_MedicineId]
    ON [dbo].[PrescriptionMedications]([MedicineId] ASC);


GO
PRINT N'Creating Index [dbo].[PrescriptionMedications].[IX_PrescriptionMedications_PrescriptionId]...';


GO
CREATE NONCLUSTERED INDEX [IX_PrescriptionMedications_PrescriptionId]
    ON [dbo].[PrescriptionMedications]([PrescriptionId] ASC);


GO
PRINT N'Creating Table [dbo].[ProcessedMessages]...';


GO
CREATE TABLE [dbo].[ProcessedMessages] (
    [EventId]        UNIQUEIDENTIFIER NOT NULL,
    [EventType]      NVARCHAR (200)   NOT NULL,
    [PrescriptionId] UNIQUEIDENTIFIER NOT NULL,
    [ProcessedAtUtc] DATETIME2 (3)    NOT NULL,
    [InsertedAt]     DATETIME2 (3)    NOT NULL,
    [InsertedBy]     UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT [PK_ProcessedMessages] PRIMARY KEY CLUSTERED ([EventId] ASC)
);


GO
PRINT N'Creating Index [dbo].[ProcessedMessages].[IX_ProcessedMessages_PrescriptionId]...';


GO
CREATE NONCLUSTERED INDEX [IX_ProcessedMessages_PrescriptionId]
    ON [dbo].[ProcessedMessages]([PrescriptionId] ASC);


GO
PRINT N'Creating Table [dbo].[TblErrorLog]...';


GO
CREATE TABLE [dbo].[TblErrorLog] (
    [ID]             UNIQUEIDENTIFIER NOT NULL,
    [Error]          NVARCHAR (200)   NOT NULL,
    [StoreProcedure] NVARCHAR (250)   NOT NULL,
    [ErrorStack]     NVARCHAR (MAX)   NOT NULL,
    [InsertedAt]     DATETIME2 (3)    NOT NULL,
    CONSTRAINT [PK_TblErrorLog] PRIMARY KEY CLUSTERED ([ID] ASC)
);


GO
PRINT N'Creating Default Constraint [Admin].[DF_PracticeLocations_Id]...';


GO
ALTER TABLE [Admin].[tblPracticeLocations]
    ADD CONSTRAINT [DF_PracticeLocations_Id] DEFAULT (newid()) FOR [Id];


GO
PRINT N'Creating Default Constraint [Admin].[DF_PracticeLocations_IsActive]...';


GO
ALTER TABLE [Admin].[tblPracticeLocations]
    ADD CONSTRAINT [DF_PracticeLocations_IsActive] DEFAULT ((1)) FOR [IsActive];


GO
PRINT N'Creating Default Constraint [Admin].[DF_PracticeLocations_InsertedAt]...';


GO
ALTER TABLE [Admin].[tblPracticeLocations]
    ADD CONSTRAINT [DF_PracticeLocations_InsertedAt] DEFAULT (sysutcdatetime()) FOR [InsertedAt];


GO
PRINT N'Creating Default Constraint [Admin].[DF_PracticeLocations_IsDeleted]...';


GO
ALTER TABLE [Admin].[tblPracticeLocations]
    ADD CONSTRAINT [DF_PracticeLocations_IsDeleted] DEFAULT ((0)) FOR [IsDeleted];


GO
PRINT N'Creating Default Constraint [Admin].[DF_Practices_IsActive]...';


GO
ALTER TABLE [Admin].[tblPractices]
    ADD CONSTRAINT [DF_Practices_IsActive] DEFAULT ((1)) FOR [IsActive];


GO
PRINT N'Creating Default Constraint [Admin].[DF_Practices_InsertedAt]...';


GO
ALTER TABLE [Admin].[tblPractices]
    ADD CONSTRAINT [DF_Practices_InsertedAt] DEFAULT (sysutcdatetime()) FOR [InsertedAt];


GO
PRINT N'Creating Default Constraint [Admin].[DF_Practices_IsDeleted]...';


GO
ALTER TABLE [Admin].[tblPractices]
    ADD CONSTRAINT [DF_Practices_IsDeleted] DEFAULT ((0)) FOR [IsDeleted];


GO
PRINT N'Creating Default Constraint [Admin].[DF_Practices_Id]...';


GO
ALTER TABLE [Admin].[tblPractices]
    ADD CONSTRAINT [DF_Practices_Id] DEFAULT (newid()) FOR [Id];


GO
PRINT N'Creating Default Constraint [Lookup].[DF_Medicines_Id]...';


GO
ALTER TABLE [Lookup].[tblMedicines]
    ADD CONSTRAINT [DF_Medicines_Id] DEFAULT (newid()) FOR [Id];


GO
PRINT N'Creating Default Constraint [Lookup].[DF_Medicines_InsertedAt]...';


GO
ALTER TABLE [Lookup].[tblMedicines]
    ADD CONSTRAINT [DF_Medicines_InsertedAt] DEFAULT (sysutcdatetime()) FOR [InsertedAt];


GO
PRINT N'Creating Default Constraint [Lookup].[DF_Medicines_IsActive]...';


GO
ALTER TABLE [Lookup].[tblMedicines]
    ADD CONSTRAINT [DF_Medicines_IsActive] DEFAULT ((1)) FOR [IsActive];


GO
PRINT N'Creating Default Constraint [Lookup].[DF_Medicines_IsDeleted]...';


GO
ALTER TABLE [Lookup].[tblMedicines]
    ADD CONSTRAINT [DF_Medicines_IsDeleted] DEFAULT ((0)) FOR [IsDeleted];


GO
PRINT N'Creating Default Constraint [Prescription].[DF_Prescriptions_Status]...';


GO
ALTER TABLE [Prescription].[tblPrescriptions]
    ADD CONSTRAINT [DF_Prescriptions_Status] DEFAULT ((0)) FOR [Status];


GO
PRINT N'Creating Default Constraint [Prescription].[DF_Prescriptions_IsActive]...';


GO
ALTER TABLE [Prescription].[tblPrescriptions]
    ADD CONSTRAINT [DF_Prescriptions_IsActive] DEFAULT ((1)) FOR [IsActive];


GO
PRINT N'Creating Default Constraint [Prescription].[DF_Prescriptions_Id]...';


GO
ALTER TABLE [Prescription].[tblPrescriptions]
    ADD CONSTRAINT [DF_Prescriptions_Id] DEFAULT (newid()) FOR [Id];


GO
PRINT N'Creating Default Constraint [Prescription].[DF_Prescriptions_IsDeleted]...';


GO
ALTER TABLE [Prescription].[tblPrescriptions]
    ADD CONSTRAINT [DF_Prescriptions_IsDeleted] DEFAULT ((0)) FOR [IsDeleted];


GO
PRINT N'Creating Default Constraint [Prescription].[DF_Prescriptions_InsertedAt]...';


GO
ALTER TABLE [Prescription].[tblPrescriptions]
    ADD CONSTRAINT [DF_Prescriptions_InsertedAt] DEFAULT (sysutcdatetime()) FOR [InsertedAt];


GO
PRINT N'Creating Default Constraint [Prescription].[DF_Prescriptions_CreatedAtUtc]...';


GO
ALTER TABLE [Prescription].[tblPrescriptions]
    ADD CONSTRAINT [DF_Prescriptions_CreatedAtUtc] DEFAULT (sysutcdatetime()) FOR [CreatedAtUtc];


GO
PRINT N'Creating Default Constraint [Profile].[DF_Providers_InsertedAt]...';


GO
ALTER TABLE [Profile].[tblProviders]
    ADD CONSTRAINT [DF_Providers_InsertedAt] DEFAULT (sysutcdatetime()) FOR [InsertedAt];


GO
PRINT N'Creating Default Constraint [Profile].[DF_Providers_IsActive]...';


GO
ALTER TABLE [Profile].[tblProviders]
    ADD CONSTRAINT [DF_Providers_IsActive] DEFAULT ((1)) FOR [IsActive];


GO
PRINT N'Creating Default Constraint [Profile].[DF_Providers_Id]...';


GO
ALTER TABLE [Profile].[tblProviders]
    ADD CONSTRAINT [DF_Providers_Id] DEFAULT (newid()) FOR [Id];


GO
PRINT N'Creating Default Constraint [Profile].[DF_Providers_IsDeleted]...';


GO
ALTER TABLE [Profile].[tblProviders]
    ADD CONSTRAINT [DF_Providers_IsDeleted] DEFAULT ((0)) FOR [IsDeleted];


GO
PRINT N'Creating Default Constraint [Profile].[DF_Patients_Id]...';


GO
ALTER TABLE [Profile].[tblPatients]
    ADD CONSTRAINT [DF_Patients_Id] DEFAULT (newid()) FOR [Id];


GO
PRINT N'Creating Default Constraint [Profile].[DF_Patients_InsertedAt]...';


GO
ALTER TABLE [Profile].[tblPatients]
    ADD CONSTRAINT [DF_Patients_InsertedAt] DEFAULT (sysutcdatetime()) FOR [InsertedAt];


GO
PRINT N'Creating Default Constraint [Profile].[DF_Patients_IsActive]...';


GO
ALTER TABLE [Profile].[tblPatients]
    ADD CONSTRAINT [DF_Patients_IsActive] DEFAULT ((1)) FOR [IsActive];


GO
PRINT N'Creating Default Constraint [Profile].[DF_Patients_IsDeleted]...';


GO
ALTER TABLE [Profile].[tblPatients]
    ADD CONSTRAINT [DF_Patients_IsDeleted] DEFAULT ((0)) FOR [IsDeleted];


GO
PRINT N'Creating Default Constraint [Profile].[DF_Users_IsDeleted]...';


GO
ALTER TABLE [Profile].[tblUsers]
    ADD CONSTRAINT [DF_Users_IsDeleted] DEFAULT ((0)) FOR [IsDeleted];


GO
PRINT N'Creating Default Constraint [Profile].[DF_Users_IsActive]...';


GO
ALTER TABLE [Profile].[tblUsers]
    ADD CONSTRAINT [DF_Users_IsActive] DEFAULT ((1)) FOR [IsActive];


GO
PRINT N'Creating Default Constraint [Profile].[DF_Users_Role]...';


GO
ALTER TABLE [Profile].[tblUsers]
    ADD CONSTRAINT [DF_Users_Role] DEFAULT ((0)) FOR [Role];


GO
PRINT N'Creating Default Constraint [Profile].[DF_Users_InsertedAt]...';


GO
ALTER TABLE [Profile].[tblUsers]
    ADD CONSTRAINT [DF_Users_InsertedAt] DEFAULT (sysutcdatetime()) FOR [InsertedAt];


GO
PRINT N'Creating Default Constraint [Profile].[DF_Users_Id]...';


GO
ALTER TABLE [Profile].[tblUsers]
    ADD CONSTRAINT [DF_Users_Id] DEFAULT (newid()) FOR [Id];


GO
PRINT N'Creating Default Constraint [dbo].[DF_PrescriptionMedications_IsActive]...';


GO
ALTER TABLE [dbo].[PrescriptionMedications]
    ADD CONSTRAINT [DF_PrescriptionMedications_IsActive] DEFAULT ((1)) FOR [IsActive];


GO
PRINT N'Creating Default Constraint [dbo].[DF_PrescriptionMedications_Id]...';


GO
ALTER TABLE [dbo].[PrescriptionMedications]
    ADD CONSTRAINT [DF_PrescriptionMedications_Id] DEFAULT (newid()) FOR [Id];


GO
PRINT N'Creating Default Constraint [dbo].[DF_PrescriptionMedications_IsDeleted]...';


GO
ALTER TABLE [dbo].[PrescriptionMedications]
    ADD CONSTRAINT [DF_PrescriptionMedications_IsDeleted] DEFAULT ((0)) FOR [IsDeleted];


GO
PRINT N'Creating Default Constraint [dbo].[DF_PrescriptionMedications_InsertedAt]...';


GO
ALTER TABLE [dbo].[PrescriptionMedications]
    ADD CONSTRAINT [DF_PrescriptionMedications_InsertedAt] DEFAULT (sysutcdatetime()) FOR [InsertedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProcessedMessages_InsertedAt]...';


GO
ALTER TABLE [dbo].[ProcessedMessages]
    ADD CONSTRAINT [DF_ProcessedMessages_InsertedAt] DEFAULT (sysutcdatetime()) FOR [InsertedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_ProcessedMessages_ProcessedAtUtc]...';


GO
ALTER TABLE [dbo].[ProcessedMessages]
    ADD CONSTRAINT [DF_ProcessedMessages_ProcessedAtUtc] DEFAULT (sysutcdatetime()) FOR [ProcessedAtUtc];


GO
PRINT N'Creating Default Constraint [dbo].[DF_TblErrorLog_InsertedAt]...';


GO
ALTER TABLE [dbo].[TblErrorLog]
    ADD CONSTRAINT [DF_TblErrorLog_InsertedAt] DEFAULT (sysutcdatetime()) FOR [InsertedAt];


GO
PRINT N'Creating Default Constraint [dbo].[DF_TblErrorLog_ID]...';


GO
ALTER TABLE [dbo].[TblErrorLog]
    ADD CONSTRAINT [DF_TblErrorLog_ID] DEFAULT (newid()) FOR [ID];


GO
PRINT N'Creating Foreign Key [Admin].[FK_PracticeLocations_InsertedBy_Users]...';


GO
ALTER TABLE [Admin].[tblPracticeLocations]
    ADD CONSTRAINT [FK_PracticeLocations_InsertedBy_Users] FOREIGN KEY ([InsertedBy]) REFERENCES [Profile].[tblUsers] ([Id]);


GO
PRINT N'Creating Foreign Key [Admin].[FK_PracticeLocations_UpdatedBy_Users]...';


GO
ALTER TABLE [Admin].[tblPracticeLocations]
    ADD CONSTRAINT [FK_PracticeLocations_UpdatedBy_Users] FOREIGN KEY ([UpdatedBy]) REFERENCES [Profile].[tblUsers] ([Id]);


GO
PRINT N'Creating Foreign Key [Admin].[FK_PracticeLocations_Practices]...';


GO
ALTER TABLE [Admin].[tblPracticeLocations]
    ADD CONSTRAINT [FK_PracticeLocations_Practices] FOREIGN KEY ([PracticeId]) REFERENCES [Admin].[tblPractices] ([Id]);


GO
PRINT N'Creating Foreign Key [Admin].[FK_Practices_InsertedBy_tblUsers]...';


GO
ALTER TABLE [Admin].[tblPractices]
    ADD CONSTRAINT [FK_Practices_InsertedBy_tblUsers] FOREIGN KEY ([InsertedBy]) REFERENCES [Profile].[tblUsers] ([Id]);


GO
PRINT N'Creating Foreign Key [Admin].[FK_Practices_UpdatedBy_tblUsers]...';


GO
ALTER TABLE [Admin].[tblPractices]
    ADD CONSTRAINT [FK_Practices_UpdatedBy_tblUsers] FOREIGN KEY ([UpdatedBy]) REFERENCES [Profile].[tblUsers] ([Id]);


GO
PRINT N'Creating Foreign Key [Lookup].[FK_Medicines_UpdatedBy_Users]...';


GO
ALTER TABLE [Lookup].[tblMedicines]
    ADD CONSTRAINT [FK_Medicines_UpdatedBy_Users] FOREIGN KEY ([UpdatedBy]) REFERENCES [Profile].[tblUsers] ([Id]);


GO
PRINT N'Creating Foreign Key [Lookup].[FK_Medicines_InsertedBy_Users]...';


GO
ALTER TABLE [Lookup].[tblMedicines]
    ADD CONSTRAINT [FK_Medicines_InsertedBy_Users] FOREIGN KEY ([InsertedBy]) REFERENCES [Profile].[tblUsers] ([Id]);


GO
PRINT N'Creating Foreign Key [Prescription].[FK_Prescriptions_UpdatedBy_Users]...';


GO
ALTER TABLE [Prescription].[tblPrescriptions]
    ADD CONSTRAINT [FK_Prescriptions_UpdatedBy_Users] FOREIGN KEY ([UpdatedBy]) REFERENCES [Profile].[tblUsers] ([Id]);


GO
PRINT N'Creating Foreign Key [Prescription].[FK_Prescriptions_PracticeLocations]...';


GO
ALTER TABLE [Prescription].[tblPrescriptions]
    ADD CONSTRAINT [FK_Prescriptions_PracticeLocations] FOREIGN KEY ([PracticeLocationId]) REFERENCES [Admin].[tblPracticeLocations] ([Id]);


GO
PRINT N'Creating Foreign Key [Prescription].[FK_Prescriptions_RepeatOf]...';


GO
ALTER TABLE [Prescription].[tblPrescriptions]
    ADD CONSTRAINT [FK_Prescriptions_RepeatOf] FOREIGN KEY ([RepeatOfPrescriptionId]) REFERENCES [Prescription].[tblPrescriptions] ([Id]);


GO
PRINT N'Creating Foreign Key [Prescription].[FK_Prescriptions_Patients]...';


GO
ALTER TABLE [Prescription].[tblPrescriptions]
    ADD CONSTRAINT [FK_Prescriptions_Patients] FOREIGN KEY ([PatientId]) REFERENCES [Profile].[tblPatients] ([Id]);


GO
PRINT N'Creating Foreign Key [Prescription].[FK_Prescriptions_InsertedBy_Users]...';


GO
ALTER TABLE [Prescription].[tblPrescriptions]
    ADD CONSTRAINT [FK_Prescriptions_InsertedBy_Users] FOREIGN KEY ([InsertedBy]) REFERENCES [Profile].[tblUsers] ([Id]);


GO
PRINT N'Creating Foreign Key [Prescription].[FK_Prescriptions_Providers]...';


GO
ALTER TABLE [Prescription].[tblPrescriptions]
    ADD CONSTRAINT [FK_Prescriptions_Providers] FOREIGN KEY ([ProviderId]) REFERENCES [Profile].[tblProviders] ([Id]);


GO
PRINT N'Creating Foreign Key [Profile].[FK_Providers_UpdatedBy_Users]...';


GO
ALTER TABLE [Profile].[tblProviders]
    ADD CONSTRAINT [FK_Providers_UpdatedBy_Users] FOREIGN KEY ([UpdatedBy]) REFERENCES [Profile].[tblUsers] ([Id]);


GO
PRINT N'Creating Foreign Key [Profile].[FK_Providers_InsertedBy_Users]...';


GO
ALTER TABLE [Profile].[tblProviders]
    ADD CONSTRAINT [FK_Providers_InsertedBy_Users] FOREIGN KEY ([InsertedBy]) REFERENCES [Profile].[tblUsers] ([Id]);


GO
PRINT N'Creating Foreign Key [Profile].[FK_Providers_PracticeLocations]...';


GO
ALTER TABLE [Profile].[tblProviders]
    ADD CONSTRAINT [FK_Providers_PracticeLocations] FOREIGN KEY ([PracticeLocationId]) REFERENCES [Admin].[tblPracticeLocations] ([Id]);


GO
PRINT N'Creating Foreign Key [Profile].[FK_Patients_UpdatedBy_Users]...';


GO
ALTER TABLE [Profile].[tblPatients]
    ADD CONSTRAINT [FK_Patients_UpdatedBy_Users] FOREIGN KEY ([UpdatedBy]) REFERENCES [Profile].[tblUsers] ([Id]);


GO
PRINT N'Creating Foreign Key [Profile].[FK_Patients_InsertedBy_Users]...';


GO
ALTER TABLE [Profile].[tblPatients]
    ADD CONSTRAINT [FK_Patients_InsertedBy_Users] FOREIGN KEY ([InsertedBy]) REFERENCES [Profile].[tblUsers] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_PrescriptionMedications_UpdatedBy_Users]...';


GO
ALTER TABLE [dbo].[PrescriptionMedications]
    ADD CONSTRAINT [FK_PrescriptionMedications_UpdatedBy_Users] FOREIGN KEY ([UpdatedBy]) REFERENCES [Profile].[tblUsers] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_PrescriptionMedications_Medicines]...';


GO
ALTER TABLE [dbo].[PrescriptionMedications]
    ADD CONSTRAINT [FK_PrescriptionMedications_Medicines] FOREIGN KEY ([MedicineId]) REFERENCES [Lookup].[tblMedicines] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_PrescriptionMedications_InsertedBy_Users]...';


GO
ALTER TABLE [dbo].[PrescriptionMedications]
    ADD CONSTRAINT [FK_PrescriptionMedications_InsertedBy_Users] FOREIGN KEY ([InsertedBy]) REFERENCES [Profile].[tblUsers] ([Id]);


GO
PRINT N'Creating Foreign Key [dbo].[FK_PrescriptionMedications_Prescriptions]...';


GO
ALTER TABLE [dbo].[PrescriptionMedications]
    ADD CONSTRAINT [FK_PrescriptionMedications_Prescriptions] FOREIGN KEY ([PrescriptionId]) REFERENCES [Prescription].[tblPrescriptions] ([Id]) ON DELETE CASCADE;


GO
PRINT N'Creating Foreign Key [dbo].[FK_ProcessedMessages_InsertedBy_Users]...';


GO
ALTER TABLE [dbo].[ProcessedMessages]
    ADD CONSTRAINT [FK_ProcessedMessages_InsertedBy_Users] FOREIGN KEY ([InsertedBy]) REFERENCES [Profile].[tblUsers] ([Id]);


GO
PRINT N'Creating Check Constraint [Admin].[CK_PracticeLocations_HpiNo]...';


GO
ALTER TABLE [Admin].[tblPracticeLocations]
    ADD CONSTRAINT [CK_PracticeLocations_HpiNo] CHECK ([HpiNo] like '[A-Z][A-Z][A-Z][0-9][0-9]');


GO
PRINT N'Creating Check Constraint [Admin].[CK_PracticeLocations_HpiExtension]...';


GO
ALTER TABLE [Admin].[tblPracticeLocations]
    ADD CONSTRAINT [CK_PracticeLocations_HpiExtension] CHECK ([HpiExtension] like '[A-Z]');


GO
PRINT N'Creating Check Constraint [Prescription].[CK_Prescriptions_Scid]...';


GO
ALTER TABLE [Prescription].[tblPrescriptions]
    ADD CONSTRAINT [CK_Prescriptions_Scid] CHECK ([Scid] like '9__________' AND len([Scid])=(11));


GO
PRINT N'Creating Check Constraint [Prescription].[CK_Prescriptions_Status]...';


GO
ALTER TABLE [Prescription].[tblPrescriptions]
    ADD CONSTRAINT [CK_Prescriptions_Status] CHECK ([Status]>=(0) AND [Status]<=(5));


GO
PRINT N'Creating Check Constraint [Profile].[CK_Providers_Type]...';


GO
ALTER TABLE [Profile].[tblProviders]
    ADD CONSTRAINT [CK_Providers_Type] CHECK ([Type]=(2) OR [Type]=(1) OR [Type]=(0));


GO
PRINT N'Creating Check Constraint [Profile].[CK_Patients_Nhi]...';


GO
ALTER TABLE [Profile].[tblPatients]
    ADD CONSTRAINT [CK_Patients_Nhi] CHECK ([Nhi] like '[A-Z][A-Z][A-Z][0-9][0-9][0-9][0-9]');


GO
PRINT N'Creating Check Constraint [Profile].[CK_Users_Role]...';


GO
ALTER TABLE [Profile].[tblUsers]
    ADD CONSTRAINT [CK_Users_Role] CHECK ([Role]=(1) OR [Role]=(0));


GO
PRINT N'Creating Check Constraint [dbo].[CK_PrescriptionMedications_Quantity]...';


GO
ALTER TABLE [dbo].[PrescriptionMedications]
    ADD CONSTRAINT [CK_PrescriptionMedications_Quantity] CHECK ([Quantity]>(0));


