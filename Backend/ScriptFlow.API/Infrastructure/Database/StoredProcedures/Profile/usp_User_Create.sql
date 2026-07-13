-- InsertedBy is deliberately set to the new user's own Id: Profile.tblUsers carries no FK
-- on InsertedBy/UpdatedBy (see DatabaseSpec.md), specifically to avoid a bootstrap
-- chicken-and-egg problem for the very first user, so every user "inserts itself".
CREATE OR ALTER PROCEDURE Profile.usp_User_Create
    @Id           UNIQUEIDENTIFIER,
    @Email        NVARCHAR(256),
    @PasswordHash NVARCHAR(512),
    @Role         TINYINT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        INSERT INTO Profile.tblUsers (Id, Email, PasswordHash, Role, InsertedBy)
        VALUES (@Id, @Email, @PasswordHash, @Role, @Id);
    END TRY
    BEGIN CATCH
        INSERT INTO dbo.TblErrorLog (ID, Error, StoreProcedure, ErrorStack, InsertedAt)
        VALUES (
            NEWID(),
            LEFT(ERROR_MESSAGE(), 200),
            'Profile.usp_User_Create',
            CONCAT('Number=', ERROR_NUMBER(), '; Severity=', ERROR_SEVERITY(), '; State=', ERROR_STATE(),
                   '; Line=', ERROR_LINE(), '; Procedure=', ERROR_PROCEDURE(), '; Message=', ERROR_MESSAGE()),
            SYSUTCDATETIME()
        );

        THROW;
    END CATCH
END
