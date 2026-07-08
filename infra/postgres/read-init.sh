#!/bin/bash
set -e

echo "Creating databases on read-db..."
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
    CREATE DATABASE "OrderDb";
    CREATE DATABASE "PaymentDb";
    CREATE DATABASE "OrderDb-USA";
    CREATE DATABASE "PaymentDb-USA";

EOSQL

echo "Read-DB init complete."
