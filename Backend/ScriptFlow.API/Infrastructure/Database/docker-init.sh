#!/usr/bin/env bash
# One-shot database bootstrap for docker-compose (see docker-compose.yml's db-init service).
# Runs once against a brand-new sqlserver container: schema -> system user -> stored procedures
# -> lookup/master seed data. Idempotent as a whole (each piece is itself idempotent or only
# ever runs once per fresh volume), so re-running docker-compose up against an already-seeded
# volume is safe - CreateSchema is the only genuinely non-idempotent piece, and it's guarded
# by checking whether Profile.tblUsers (the last table it creates a dependency on) already exists.
set -euo pipefail

SCRIPTS_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

if command -v sqlcmd >/dev/null 2>&1; then
    SQLCMD=sqlcmd
elif [ -x /opt/mssql-tools18/bin/sqlcmd ]; then
    SQLCMD=/opt/mssql-tools18/bin/sqlcmd
else
    SQLCMD=/opt/mssql-tools/bin/sqlcmd
fi

# -C: trust the container's self-signed cert (mssql-tools18 defaults to encrypted connections).
run_sql() {
    "$SQLCMD" -S "$SQLSERVER_HOST" -U sa -P "$MSSQL_SA_PASSWORD" -C -b "$@"
}

echo "Waiting for SQL Server at $SQLSERVER_HOST..."
until run_sql -Q "SELECT 1" >/dev/null 2>&1; do
    sleep 2
done

echo "Checking whether ScriptFlow database is already bootstrapped..."
# Two separate queries, deliberately: referencing ScriptFlow.Profile.tblUsers in a single query
# fails to even PARSE when the ScriptFlow database doesn't exist yet (a truly fresh server) -
# SQL Server resolves three-part names at compile time, before the IF's short-circuit ever runs
# at runtime. So check DB_ID() first (safe against master, no dependency on ScriptFlow existing),
# and only reference Profile.tblUsers in a second query once already connected inside that DB.
DB_EXISTS=$(run_sql -h -1 -Q "SET NOCOUNT ON; SELECT CASE WHEN DB_ID('ScriptFlow') IS NOT NULL THEN 1 ELSE 0 END" | tr -d '[:space:]')

ALREADY_DONE=0
if [ "$DB_EXISTS" = "1" ]; then
    ALREADY_DONE=$(run_sql -d ScriptFlow -h -1 -Q "SET NOCOUNT ON; IF EXISTS (SELECT 1 FROM Profile.tblUsers) SELECT 1 ELSE SELECT 0" | tr -d '[:space:]')
fi

if [ "$ALREADY_DONE" = "1" ]; then
    echo "ScriptFlow database already bootstrapped (persisted volume) - skipping."
    exit 0
fi

echo "Creating schema..."
run_sql -i "$SCRIPTS_DIR/Schema/00_CreateSchema.sql"

echo "Seeding system user..."
run_sql -d ScriptFlow -i "$SCRIPTS_DIR/Schema/01_SeedSystemUser.sql"

echo "Deploying stored procedures..."
for f in "$SCRIPTS_DIR"/StoredProcedures/*/*.sql; do
    echo "  $f"
    run_sql -d ScriptFlow -i "$f"
done

echo "Seeding lookup/master data..."
run_sql -d ScriptFlow -i "$SCRIPTS_DIR/Performance/01_ExpandLookupData.sql"

echo "Database bootstrap complete."
