-- Backs IMedicineRepository.GetManyAsync's bulk lookup, used by every prescription
-- handler to resolve medicine names for the returned DTOs.
CREATE OR ALTER PROCEDURE Lookup.usp_Medicine_GetByIds
    @Ids dbo.tvpGuidList READONLY
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        SELECT
            m.Id,
            m.Name,
            m.Sctid,
            m.Form,
            m.Type
        FROM Lookup.tblMedicines m
        INNER JOIN @Ids i ON i.Id = m.Id
        WHERE m.IsDeleted = 0;
    END TRY
    BEGIN CATCH
        INSERT INTO dbo.TblErrorLog (ID, Error, StoreProcedure, ErrorStack, InsertedAt)
        VALUES (
            NEWID(),
            LEFT(ERROR_MESSAGE(), 200),
            'Lookup.usp_Medicine_GetByIds',
            CONCAT('Number=', ERROR_NUMBER(), '; Severity=', ERROR_SEVERITY(), '; State=', ERROR_STATE(),
                   '; Line=', ERROR_LINE(), '; Procedure=', ERROR_PROCEDURE(), '; Message=', ERROR_MESSAGE()),
            SYSUTCDATETIME()
        );

        THROW;
    END CATCH
END
