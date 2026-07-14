-- Backs SqlProcessedMessageStore.IsProcessedAsync - checked before handling every integration
-- event that has a real side effect (a pharmacy call, a status write), so a RabbitMQ
-- redelivery after a crash never repeats it.
CREATE OR ALTER PROCEDURE dbo.usp_ProcessedMessage_IsProcessed
    @EventId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        SELECT CASE WHEN EXISTS (
            SELECT 1 FROM dbo.ProcessedMessages WHERE EventId = @EventId
        ) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END;
    END TRY
    BEGIN CATCH
        INSERT INTO dbo.TblErrorLog (ID, Error, StoreProcedure, ErrorStack, InsertedAt)
        VALUES (
            NEWID(),
            LEFT(ERROR_MESSAGE(), 200),
            'dbo.usp_ProcessedMessage_IsProcessed',
            CONCAT('Number=', ERROR_NUMBER(), '; Severity=', ERROR_SEVERITY(), '; State=', ERROR_STATE(),
                   '; Line=', ERROR_LINE(), '; Procedure=', ERROR_PROCEDURE(), '; Message=', ERROR_MESSAGE()),
            SYSUTCDATETIME()
        );

        THROW;
    END CATCH
END
