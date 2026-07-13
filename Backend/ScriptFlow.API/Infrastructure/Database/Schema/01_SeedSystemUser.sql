-- The well-known "system" row in Profile.tblUsers (Domain/Entities/SystemUser.cs) that
-- attributes writes with no interactive user behind them (a RabbitMQ consumer moving a
-- prescription to Acknowledged/Rejected/Expired, or - see Performance/01_ExpandLookupData.sql -
-- seed/master data). Every other seed script's InsertedBy FK depends on this row existing
-- first; run this immediately after Schema/00_CreateSchema.sql, before anything else.
--
-- Idempotent: safe to re-run.
IF NOT EXISTS (SELECT 1 FROM Profile.tblUsers WHERE Id = '00000000-0000-0000-0000-0000000000AA')
BEGIN
    INSERT INTO Profile.tblUsers (Id, Email, PasswordHash, Role, InsertedBy)
    VALUES (
        '00000000-0000-0000-0000-0000000000AA',
        'system@scriptflow.local',
        -- Never used to authenticate (nothing logs in as this user) - a fixed non-null
        -- placeholder satisfies the NOT NULL constraint without implying a real password exists.
        'SYSTEM_USER_NO_LOGIN',
        0, -- Prescriber
        '00000000-0000-0000-0000-0000000000AA' -- self-referential: the system user inserted itself
    );
END
GO
