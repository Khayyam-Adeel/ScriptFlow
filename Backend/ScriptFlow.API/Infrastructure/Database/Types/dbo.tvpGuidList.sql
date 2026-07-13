-- Table-valued parameter: a flat list of ids, used for "get many by id" bulk lookups
-- (e.g. Lookup.usp_Medicine_GetByIds), passed from Dapper as a DataTable/IEnumerable<SqlDataRecord>.
CREATE TYPE dbo.tvpGuidList AS TABLE
(
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY
);
