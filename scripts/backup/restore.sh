#!/bin/sh
# Restores a backup taken by backup.sh into PGDATABASE, replacing its current contents.
#   restore.sh                          restore the latest backup
#   restore.sh <name>.dump              restore a specific backup from BACKUP_DIR
set -eu

: "${PGDATABASE:?PGDATABASE is required}"
BACKUP_DIR="${BACKUP_DIR:-/backups}"

PGPASSWORD="$(cat /run/secrets/postgres_password)"
export PGPASSWORD

if [ $# -gt 0 ]; then
    file="${BACKUP_DIR}/$(basename "$1")"
else
    file="$(ls -1 "${BACKUP_DIR}"/*.dump 2>/dev/null | sort | tail -n 1 || true)"
fi

if [ -z "${file}" ] || [ ! -f "${file}" ]; then
    echo "backup not found: ${file:-<none in ${BACKUP_DIR}>}" >&2
    exit 1
fi

echo "restoring ${file} into ${PGDATABASE}"
pg_restore --clean --if-exists --no-owner --single-transaction --exit-on-error --dbname="${PGDATABASE}" "${file}"
echo "restore completed"
