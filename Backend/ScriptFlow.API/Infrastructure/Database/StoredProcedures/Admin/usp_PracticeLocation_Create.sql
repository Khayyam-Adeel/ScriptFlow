CREATE OR ALTER PROCEDURE Admin.usp_PracticeLocation_Create
    @Id           UNIQUEIDENTIFIER,
    @PracticeId   UNIQUEIDENTIFIER,
    @Name         NVARCHAR(200),
    @HpiNo        CHAR(5),
    @HpiExtension CHAR(1),
    @Address      NVARCHAR(500),
    @Phone        NVARCHAR(20),
    @InsertedBy   UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        INSERT INTO Admin.tblPracticeLocations (Id, PracticeId, Name, HpiNo, HpiExtension, Address, Phone, InsertedBy)
        VALUES (@Id, @PracticeId, @Name, @HpiNo, @HpiExtension, @Address, @Phone, @InsertedBy);
    END TRY
    BEGIN CATCH
        INSERT INTO dbo.TblErrorLog (ID, Error, StoreProcedure, ErrorStack, InsertedAt)
        VALUES (
            NEWID(),
            LEFT(ERROR_MESSAGE(), 200),
            'Admin.usp_PracticeLocation_Create',
            CONCAT('Number=', ERROR_NUMBER(), '; Severity=', ERROR_SEVERITY(), '; State=', ERROR_STATE(),
                   '; Line=', ERROR_LINE(), '; Procedure=', ERROR_PROCEDURE(), '; Message=', ERROR_MESSAGE()),
            SYSUTCDATETIME()
        );

        THROW;
    END CATCH
END
