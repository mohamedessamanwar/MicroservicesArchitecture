#!/bin/sh
set -e

echo "Starting PostgreSQL replica container in background..."
docker-entrypoint.sh postgres "$@" &
PG_PID=$!

echo "Waiting for PostgreSQL replica to be ready..."
until pg_isready -U "$POSTGRES_USER" -d template1 -h 127.0.0.1 -p 5432 -q; do
    sleep 1
done

echo "PostgreSQL replica ready. Verifying/creating databases idempotently..."
psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "$POSTGRES_DB" <<-EOSQL
    SELECT 'CREATE DATABASE "OrderDb"' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'OrderDb')\gexec
    SELECT 'CREATE DATABASE "PaymentDb"' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'PaymentDb')\gexec
    SELECT 'CREATE DATABASE "OrderDb-USA"' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'OrderDb-USA')\gexec
    SELECT 'CREATE DATABASE "PaymentDb-USA"' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'PaymentDb-USA')\gexec
EOSQL

echo "Applying idempotent table schema migrations on read-db databases for logical replication compatibility..."
if [ -f /scripts/order-migration.sql ]; then
    psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "OrderDb" -f /scripts/order-migration.sql
    psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "OrderDb-USA" -f /scripts/order-migration.sql
fi

if [ -f /scripts/payment-migration.sql ]; then
    psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "PaymentDb" -f /scripts/payment-migration.sql
    psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "PaymentDb-USA" -f /scripts/payment-migration.sql
fi

echo "Read-DB startup wrapper complete. Waiting for main PostgreSQL process..."
wait "$PG_PID"
