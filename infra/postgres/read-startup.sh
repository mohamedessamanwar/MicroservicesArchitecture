#!/bin/sh
set -e

echo "Starting PostgreSQL replica container in background..."
if [ "$1" = "postgres" ]; then
    docker-entrypoint.sh "$@" &
else
    docker-entrypoint.sh postgres "$@" &
fi
PG_PID=$!

echo "Waiting for PostgreSQL replica to be ready..."
until pg_isready -U "$POSTGRES_USER" -d template1 -h 127.0.0.1 -p 5432 -q; do
    sleep 1
done

echo "Configuring pg_hba.conf for secure password authentication inside Docker network..."
# SECURITY NOTE: For local development within Docker network, password authentication (scram-sha-256) is enabled.
# In production, DO NOT use 'trust'. Restrict host connections using 'scram-sha-256', TLS certificates, and strict CIDR blocks.
grep -q "host all all all scram-sha-256" "$PGDATA/pg_hba.conf" || echo "host all all all scram-sha-256" >> "$PGDATA/pg_hba.conf"
grep -q "host replication all all scram-sha-256" "$PGDATA/pg_hba.conf" || echo "host replication all all scram-sha-256" >> "$PGDATA/pg_hba.conf"
psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d template1 -c "SELECT pg_reload_conf();"

echo "PostgreSQL replica ready. Verifying/creating databases idempotently..."
psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d template1 <<-EOSQL
    SELECT 'CREATE DATABASE "write_db"' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'write_db')\gexec
    SELECT 'CREATE DATABASE "OrderDb"' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'OrderDb')\gexec
    SELECT 'CREATE DATABASE "PaymentDb"' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'PaymentDb')\gexec
    SELECT 'CREATE DATABASE "OrderDb-USA"' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'OrderDb-USA')\gexec
    SELECT 'CREATE DATABASE "PaymentDb-USA"' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'PaymentDb-USA')\gexec
EOSQL

# NOTE: Application schema migrations (order-migration.sql / payment-migration.sql) are NO LONGER executed here.
# Schema migrations are handled exclusively by dedicated EF Core Migration Bundle containers.

echo "Read-DB startup wrapper complete. Waiting for main PostgreSQL process..."
wait "$PG_PID"
