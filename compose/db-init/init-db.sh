#!/usr/bin/env sh
set -eu

echo "Creating databases on write-db if missing..."

psql -h write-db -U admin -d postgres -tc "SELECT 1 FROM pg_database WHERE datname = 'OrderDb'" | grep -q 1 \
  || psql -h write-db -U admin -d postgres -c 'CREATE DATABASE "OrderDb";'

psql -h write-db -U admin -d postgres -tc "SELECT 1 FROM pg_database WHERE datname = 'PaymentDb'" | grep -q 1 \
  || psql -h write-db -U admin -d postgres -c 'CREATE DATABASE "PaymentDb";'


echo "Creating databases on read-db if missing..."

psql -h read-db -U admin -d postgres -tc "SELECT 1 FROM pg_database WHERE datname = 'OrderDb'" | grep -q 1 \
  || psql -h read-db -U admin -d postgres -c 'CREATE DATABASE "OrderDb";'

psql -h read-db -U admin -d postgres -tc "SELECT 1 FROM pg_database WHERE datname = 'PaymentDb'" | grep -q 1 \
  || psql -h read-db -U admin -d postgres -c 'CREATE DATABASE "PaymentDb";'


echo "Creating publications on write-db..."

psql -h write-db -U admin -d OrderDb -c "CREATE PUBLICATION order_pub FOR ALL TABLES;" || true
psql -h write-db -U admin -d PaymentDb -c "CREATE PUBLICATION payment_pub FOR ALL TABLES;" || true

echo "Done."