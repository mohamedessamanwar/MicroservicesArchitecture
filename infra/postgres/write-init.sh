#!/bin/bash
set -e

echo "Creating databases on write-db..."
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
    CREATE DATABASE "OrderDb";
    CREATE DATABASE "PaymentDb";
    CREATE DATABASE "OrderDb-USA";
    CREATE DATABASE "PaymentDb-USA";

EOSQL

echo "Creating publications on write-db..."
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "OrderDb" <<-EOSQL
    CREATE PUBLICATION order_pub FOR ALL TABLES;
EOSQL
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "PaymentDb" <<-EOSQL
    CREATE PUBLICATION payment_pub FOR ALL TABLES;
EOSQL

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "OrderDb-USA" <<-EOSQL
    CREATE PUBLICATION order_pub_usa FOR ALL TABLES;
EOSQL
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "PaymentDb-USA" <<-EOSQL
    CREATE PUBLICATION payment_pub_usa FOR ALL TABLES;
EOSQL



echo "Write-DB init complete."
