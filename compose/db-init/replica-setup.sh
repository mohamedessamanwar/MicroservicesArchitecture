#!/usr/bin/env sh
set -eu

echo "Waiting for OrderDb schema on read-db..."
until psql -h read-db -U admin -d OrderDb -Atqc "SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'Orders';" | grep -q 1; do
  echo "Waiting for tables on read-db..."
  sleep 5
done

echo "Checking for subscription 'order_sub'..."
if psql -h read-db -U admin -d OrderDb -Atqc "SELECT 1 FROM pg_subscription WHERE subname = 'order_sub';" | grep -q 1; then
  echo "Subscription order_sub already exists. Skipping."
  exit 0
fi

echo "Cleaning up potential stale slots on primary..."
psql -h write-db -U admin -d OrderDb -v ON_ERROR_STOP=1 -Atqc "SELECT pg_drop_replication_slot('order_sub') WHERE EXISTS (SELECT 1 FROM pg_replication_slots WHERE slot_name = 'order_sub');" || true

echo "Creating subscription order_sub..."
psql -h read-db -U admin -d OrderDb -v ON_ERROR_STOP=1 -c "CREATE SUBSCRIPTION order_sub CONNECTION 'host=write-db port=5432 user=admin password=pass dbname=OrderDb' PUBLICATION order_pub;"

echo "Logical replication subscription created."
