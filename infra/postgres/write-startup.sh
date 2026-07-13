#!/bin/sh
set -e

echo "Starting PostgreSQL primary container in background..."
docker-entrypoint.sh postgres "$@" &
PG_PID=$!

echo "Waiting for PostgreSQL primary to be ready..."
until pg_isready -U "$POSTGRES_USER" -d template1 -h 127.0.0.1 -p 5432 -q; do
    sleep 1
done

echo "PostgreSQL primary ready. Verifying/creating databases idempotently..."
psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "$POSTGRES_DB" <<-EOSQL
    SELECT 'CREATE DATABASE "OrderDb"' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'OrderDb')\gexec
    SELECT 'CREATE DATABASE "PaymentDb"' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'PaymentDb')\gexec
    SELECT 'CREATE DATABASE "OrderDb-USA"' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'OrderDb-USA')\gexec
    SELECT 'CREATE DATABASE "PaymentDb-USA"' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'PaymentDb-USA')\gexec
EOSQL

echo "Applying idempotent table schema migrations on write-db databases..."
if [ -f /scripts/order-migration.sql ]; then
    psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "OrderDb" -f /scripts/order-migration.sql
    psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "OrderDb-USA" -f /scripts/order-migration.sql
fi

if [ -f /scripts/payment-migration.sql ]; then
    psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "PaymentDb" -f /scripts/payment-migration.sql
    psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "PaymentDb-USA" -f /scripts/payment-migration.sql
fi

echo "Verifying/creating logical replication publications idempotently..."
psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "OrderDb" -c "DO \$\$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_publication WHERE pubname = 'order_pub') THEN CREATE PUBLICATION order_pub FOR ALL TABLES; END IF; END \$\$;"
psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "PaymentDb" -c "DO \$\$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_publication WHERE pubname = 'payment_pub') THEN CREATE PUBLICATION payment_pub FOR ALL TABLES; END IF; END \$\$;"
psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "OrderDb-USA" -c "DO \$\$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_publication WHERE pubname = 'order_pub_usa') THEN CREATE PUBLICATION order_pub_usa FOR ALL TABLES; END IF; END \$\$;"
psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "PaymentDb-USA" -c "DO \$\$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_publication WHERE pubname = 'payment_pub_usa') THEN CREATE PUBLICATION payment_pub_usa FOR ALL TABLES; END IF; END \$\$;"

echo "Write-DB startup wrapper complete. Waiting for main PostgreSQL process..."
wait "$PG_PID"
