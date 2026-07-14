CREATE OR ALTER PROCEDURE Profile.usp_Provider_Create
    @Id                 UNIQUEIDENTIFIER,
    @FirstName          NVARCHAR(100),
    @LastName           NVARCHAR(100),
    @Type               TINYINT,
    @NzmcNo             NVARCHAR(20),
    @PracticeLocationId UNIQUEIDENTIFIER,
    @Email              NVARCHAR(200),
    @PhoneNumber        NVARCHAR(20),
    @Qualification      NVARCHAR(200),
    @InsertedBy         UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        INSERT INTO Profile.tblProviders (Id, FirstName, LastName, Type, NzmcNo, PracticeLocationId, Email, PhoneNumber, Qualification, InsertedBy)
        VALUES (@Id, @FirstName, @LastName, @Type, @NzmcNo, @PracticeLocationId, @Email, @PhoneNumber, @Qualification, @InsertedBy);
    END TRY
    BEGIN CATCH
        INSERT INTO dbo.TblErrorLog (ID, Error, StoreProcedure, ErrorStack, InsertedAt)
        VALUES (
            NEWID(),
            LEFT(ERROR_MESSAGE(), 200),
            'Profile.usp_Provider_Create',
            CONCAT('Number=', ERROR_NUMBER(), '; Severity=', ERROR_SEVERITY(), '; State=', ERROR_STATE(),
                   '; Line=', ERROR_LINE(), '; Procedure=', ERROR_PROCEDURE(), '; Message=', ERROR_MESSAGE()),
            SYSUTCDATETIME()
        );

        THROW;
    END CATCH
END
