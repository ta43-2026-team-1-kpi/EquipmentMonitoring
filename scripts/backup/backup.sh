#!/bin/sh
# Scheduled pg_dump backups (custom format) with retention.
#   backup.sh            run forever, one backup every BACKUP_INTERVAL_SECONDS
#   backup.sh --once     take a single backup and exit
#   backup.sh --healthy  exit 0 if the last successful backup is recent enough (container healthcheck)
set -eu

: "${PGHOST:?PGHOST is required}"
: "${PGDATABASE:?PGDATABASE is required}"
: "${PGUSER:?PGUSER is required}"
BACKUP_DIR="${BACKUP_DIR:-/backups}"
BACKUP_INTERVAL_SECONDS="${BACKUP_INTERVAL_SECONDS:-86400}"
BACKUP_RETENTION_DAYS="${BACKUP_RETENTION_DAYS:-7}"
LAST_SUCCESS="${BACKUP_DIR}/.last-success"

case "${BACKUP_INTERVAL_SECONDS}${BACKUP_RETENTION_DAYS}" in
    *[!0-9]*) echo "BACKUP_INTERVAL_SECONDS and BACKUP_RETENTION_DAYS must be whole numbers" >&2; exit 2 ;;
esac

if [ "${1:-}" = "--healthy" ]; then
    # Healthy while the last success is younger than two intervals (one missed run is tolerated).
    max_age_minutes=$(( (BACKUP_INTERVAL_SECONDS * 2 + 59) / 60 ))
    [ -n "$(find "${LAST_SUCCESS}" -mmin "-${max_age_minutes}" 2>/dev/null)" ]
    exit $?
fi

PGPASSWORD="$(cat /run/secrets/postgres_password)"
export PGPASSWORD

# Explicit error handling: "set -e" is not applied inside a function called from "backup || ...".
backup() {
    file="${BACKUP_DIR}/${PGDATABASE}_$(date -u +%Y%m%dT%H%M%SZ).dump"

    # A dump is written under a temporary name, so an interrupted run never looks like a valid backup.
    if ! pg_dump --format=custom --no-owner --file="${file}.partial"; then
        rm -f "${file}.partial"
        echo "backup failed: pg_dump exited with an error" >&2
        return 1
    fi
    if ! pg_restore --list "${file}.partial" >/dev/null; then
        rm -f "${file}.partial"
        echo "backup failed: archive is not readable" >&2
        return 1
    fi

    mv "${file}.partial" "${file}" || return 1
    touch "${LAST_SUCCESS}"
    echo "backup created: ${file} ($(du -h "${file}" | cut -f1))"

    find "${BACKUP_DIR}" -name '*.dump' -type f -mmin "+$(( BACKUP_RETENTION_DAYS * 1440 ))" -print -delete \
        | sed 's/^/backup removed by retention: /'
    find "${BACKUP_DIR}" -name '*.partial' -type f -mmin +60 -delete
}

if [ "${1:-}" = "--once" ]; then
    backup
    exit $?
fi

echo "backups every ${BACKUP_INTERVAL_SECONDS}s into ${BACKUP_DIR}, keeping ${BACKUP_RETENTION_DAYS} days"
while true; do
    backup || echo "next attempt in ${BACKUP_INTERVAL_SECONDS}s" >&2
    sleep "${BACKUP_INTERVAL_SECONDS}"
done
