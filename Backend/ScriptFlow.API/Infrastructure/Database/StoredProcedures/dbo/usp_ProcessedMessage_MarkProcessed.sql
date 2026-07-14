-- Backs SqlProcessedMessageStore.MarkProcessedAsync. Guarded by IF NOT EXISTS rather than
-- relying solely on the PK constraint: the caller already checks IsProcessed first, but this
-- keeps a rare race (near-simultaneous redelivery) from surfacing as a duplicate-key exception
-- instead of a harmless no-op.
CREATE OR ALTER PROCEDURE dbo.usp_ProcessedMessage_MarkProcessed
    @EventId        UNIQUEIDENTIFIER,
    @EventType      NVARCHAR(200),
    @PrescriptionId UNIQUEIDENTIFIER,
    @InsertedBy     UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        IF NOT EXISTS (SELECT 1 FROM dbo.ProcessedMessages WHERE EventId = @EventId)
        BEGIN
            INSERT INTO dbo.ProcessedMessages (EventId, EventType, PrescriptionId, InsertedBy)
            VALUES (@EventId, @EventType, @PrescriptionId, @InsertedBy);
        END
    END TRY
    BEGIN CATCH
        INSERT INTO dbo.TblErrorLog (ID, Error, StoreProcedure, ErrorStack, InsertedAt)
        VALUES (
            NEWID(),
            LEFT(ERROR_MESSAGE(), 200),
            'dbo.usp_ProcessedMessage_MarkProcessed',
            CONCAT('Number=', ERROR_NUMBER(), '; Severity=', ERROR_SEVERITY(), '; State=', ERROR_STATE(),
                   '; Line=', ERROR_LINE(), '; Procedure=', ERROR_PROCEDURE(), '; Message=', ERROR_MESSAGE()),
            SYSUTCDATETIME()
        );

        THROW;
    END CATCH
END
