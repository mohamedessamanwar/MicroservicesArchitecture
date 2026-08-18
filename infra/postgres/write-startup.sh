#!/bin/sh
set -e

echo "Starting PostgreSQL primary container in background..."
if [ "$1" = "postgres" ]; then
    docker-entrypoint.sh "$@" &
else
    docker-entrypoint.sh postgres "$@" &
fi
PG_PID=$!

echo "Waiting for PostgreSQL primary to be ready..."
until pg_isready -U "$POSTGRES_USER" -d template1 -h 127.0.0.1 -p 5432 -q; do
    sleep 1
done

echo "Configuring pg_hba.conf for secure password authentication inside Docker network..."
# SECURITY NOTE: For local development within Docker network, password authentication (scram-sha-256) is enabled.
# In production, DO NOT use 'trust'. Restrict host connections using 'scram-sha-256', TLS certificates, and strict CIDR blocks.
grep -q "host all all all scram-sha-256" "$PGDATA/pg_hba.conf" || echo "host all all all scram-sha-256" >> "$PGDATA/pg_hba.conf"
grep -q "host replication all all scram-sha-256" "$PGDATA/pg_hba.conf" || echo "host replication all all scram-sha-256" >> "$PGDATA/pg_hba.conf"
psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d template1 -c "SELECT pg_reload_conf();"



echo "Creating replication user idempotently..."
psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d template1 -c "DO \$\$ BEGIN IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = '${POSTGRES_REPLICATION_USER:-replicator}') THEN CREATE ROLE ${POSTGRES_REPLICATION_USER:-replicator} LOGIN REPLICATION PASSWORD '${POSTGRES_REPLICATION_PASSWORD:-change_me}'; END IF; END \$\$;"


echo "PostgreSQL primary ready. Verifying/creating databases idempotently..."
psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d template1 <<-EOSQL
    SELECT 'CREATE DATABASE "write_db"' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'write_db')\gexec
    SELECT 'CREATE DATABASE "OrderDb"' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'OrderDb')\gexec
    SELECT 'CREATE DATABASE "PaymentDb"' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'PaymentDb')\gexec
    SELECT 'CREATE DATABASE "OrderDb-USA"' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'OrderDb-USA')\gexec
    SELECT 'CREATE DATABASE "PaymentDb-USA"' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'PaymentDb-USA')\gexec
EOSQL



echo "Verifying/creating logical replication publications idempotently..."
psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "OrderDb" -c "DO \$\$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_publication WHERE pubname = 'order_pub') THEN CREATE PUBLICATION order_pub FOR ALL TABLES; END IF; END \$\$;"
psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "PaymentDb" -c "DO \$\$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_publication WHERE pubname = 'payment_pub') THEN CREATE PUBLICATION payment_pub FOR ALL TABLES; END IF; END \$\$;"
psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "OrderDb-USA" -c "DO \$\$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_publication WHERE pubname = 'order_pub_usa') THEN CREATE PUBLICATION order_pub_usa FOR ALL TABLES; END IF; END \$\$;"
psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "PaymentDb-USA" -c "DO \$\$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_publication WHERE pubname = 'payment_pub_usa') THEN CREATE PUBLICATION payment_pub_usa FOR ALL TABLES; END IF; END \$\$;"

echo "Write-DB startup wrapper complete. Waiting for main PostgreSQL process..."
wait "$PG_PID"
