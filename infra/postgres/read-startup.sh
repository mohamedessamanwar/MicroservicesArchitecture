#!/bin/sh
set -e

if [ ! -s "$PGDATA/PG_VERSION" ]; then
    echo "Data directory is empty. Initializing physical standby from primary..."
    
    # Wait for primary to be available
    echo "Waiting for primary database to become available..."
    until pg_isready -h "${POSTGRES_PRIMARY_HOST:-write-db}" -p "${POSTGRES_PRIMARY_PORT:-5432}" -U "${POSTGRES_USER:-admin}"; do
      sleep 2
    done
    
    if [ "$(id -u)" = '0' ]; then
        export PGPASSWORD="${POSTGRES_REPLICATION_PASSWORD:-change_me}"
        SLOT_NAME="${POSTGRES_REPLICATION_SLOT:-replica_1_slot}"
        echo "Creating/Using replication slot: $SLOT_NAME"
        pg_basebackup -h "${POSTGRES_PRIMARY_HOST:-write-db}" -p "${POSTGRES_PRIMARY_PORT:-5432}" -U "${POSTGRES_REPLICATION_USER:-replicator}" -D "$PGDATA" -Fp -Xs -P -R -c fast --create-slot -S "$SLOT_NAME" || pg_basebackup -h "${POSTGRES_PRIMARY_HOST:-write-db}" -p "${POSTGRES_PRIMARY_PORT:-5432}" -U "${POSTGRES_REPLICATION_USER:-replicator}" -D "$PGDATA" -Fp -Xs -P -R -c fast -S "$SLOT_NAME"
        chown -R postgres:postgres "$PGDATA"
        chmod 700 "$PGDATA"
    else
        export PGPASSWORD="${POSTGRES_REPLICATION_PASSWORD:-change_me}"
        SLOT_NAME="${POSTGRES_REPLICATION_SLOT:-replica_1_slot}"
        echo "Creating/Using replication slot: $SLOT_NAME"
        pg_basebackup -h "${POSTGRES_PRIMARY_HOST:-write-db}" -p "${POSTGRES_PRIMARY_PORT:-5432}" -U "${POSTGRES_REPLICATION_USER:-replicator}" -D "$PGDATA" -Fp -Xs -P -R -c fast --create-slot -S "$SLOT_NAME" || pg_basebackup -h "${POSTGRES_PRIMARY_HOST:-write-db}" -p "${POSTGRES_PRIMARY_PORT:-5432}" -U "${POSTGRES_REPLICATION_USER:-replicator}" -D "$PGDATA" -Fp -Xs -P -R -c fast -S "$SLOT_NAME"
        chmod 700 "$PGDATA"
    fi
    echo "Base backup complete."
else
    echo "Data directory is not empty. Assuming replica is already initialized."
fi

echo "Starting PostgreSQL replica container..."
if [ "$1" = "postgres" ]; then
    exec docker-entrypoint.sh "$@"
else
    exec docker-entrypoint.sh postgres "$@"
fi
