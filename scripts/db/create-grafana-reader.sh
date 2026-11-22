#!/bin/sh
# Creates (or updates) the read-only "grafana_reader" role used by Grafana dashboards.
# Idempotent: safe to run on every stack start. Must run after API migrations created the tables.
set -eu

: "${PGDATABASE:?PGDATABASE is required}"
: "${PGUSER:?PGUSER is required}"

PGPASSWORD="$(cat /run/secrets/postgres_password)"
export PGPASSWORD
reader_password="$(cat /run/secrets/grafana_db_password)"

psql -v ON_ERROR_STOP=1 -q \
    -v reader_password="${reader_password}" \
    -v database="${PGDATABASE}" \
    -v owner="${PGUSER}" <<'SQL'
SELECT NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'grafana_reader') AS create_role \gset
\if :create_role
CREATE ROLE grafana_reader LOGIN;
\endif

ALTER ROLE grafana_reader WITH LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE PASSWORD :'reader_password';
ALTER ROLE grafana_reader SET default_transaction_read_only = on;
ALTER ROLE grafana_reader SET statement_timeout = '10s';

GRANT CONNECT ON DATABASE :"database" TO grafana_reader;
GRANT USAGE ON SCHEMA public TO grafana_reader;
GRANT SELECT ON ALL TABLES IN SCHEMA public TO grafana_reader;
ALTER DEFAULT PRIVILEGES FOR ROLE :"owner" IN SCHEMA public GRANT SELECT ON TABLES TO grafana_reader;
SQL

echo "grafana_reader is ready (read-only access to ${PGDATABASE})"
