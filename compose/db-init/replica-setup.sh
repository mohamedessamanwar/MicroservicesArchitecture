#!/usr/bin/env sh
set -eu

echo "Checking for subscription 'order_sub'..."
if psql -h read-db -U admin -d OrderDb -Atqc "SELECT 1 FROM pg_subscription WHERE subname = 'order_sub';" | grep -q 1; then
  echo "Subscription order_sub already exists. Skipping."
  return 0
fi
echo "Cleaning up potential stale slots on primary..."
psql -h write-db -U admin -d OrderDb -v ON_ERROR_STOP=1 -Atqc "SELECT pg_drop_replication_slot('order_sub') WHERE EXISTS (SELECT 1 FROM pg_replication_slots WHERE slot_name = 'order_sub');" || true

echo "Creating subscription order_sub..."
psql -h read-db -U admin -d OrderDb -v ON_ERROR_STOP=1 -c "CREATE SUBSCRIPTION order_sub CONNECTION 'host=write-db port=5432 user=admin password=pass dbname=OrderDb' PUBLICATION order_pub;"


if psql -h read-db -U admin -d OrderDb -Atqc "SELECT 1 FROM pg_subscription WHERE subname = 'order_sub-usa';" | grep -q 1; then
  echo "Subscription order_sub-usa already exists. Skipping."
 return 0
fi
echo "Cleaning up potential stale slots on primary..."
psql -h write-db -U admin -d OrderDb -v ON_ERROR_STOP=1 -Atqc "SELECT pg_drop_replication_slot('order_sub-usa') WHERE EXISTS (SELECT 1 FROM pg_replication_slots WHERE slot_name = 'order_sub-usa');" || true

echo "Creating subscription order_sub-usa..."
psql -h read-db -U admin -d OrderDb -v ON_ERROR_STOP=1 -c "CREATE SUBSCRIPTION order_sub CONNECTION 'host=write-db port=5432 user=admin password=pass dbname=OrderDb' PUBLICATION order_pub;"



echo "Checking for subscription 'payment_sub'..."
if psql -h read-db -U admin -d PaymentDb -Atqc "SELECT 1 FROM pg_subscription WHERE subname = 'payment_sub';" | grep -q 1; then
  echo "Subscription payment_sub already exists. Skipping."
  return 0
fi
  echo "Creating subscription payment_sub..."
  psql -h read-db -U admin -d PaymentDb -v ON_ERROR_STOP=1 -c "CREATE SUBSCRIPTION payment_sub CONNECTION 'host=write-db port=5432 user=admin password=pass dbname=PaymentDb' PUBLICATION payment_pub;"
fi


echo "Checking for subscription 'payment_sub'..."
if psql -h read-db -U admin -d PaymentDb -Atqc "SELECT 1 FROM pg_subscription WHERE subname = 'payment_sub-usa';" | grep -q 1; then
  echo "Subscription payment_sub-usa already exists. Skipping."
  return 0
fi
  echo "Creating subscription payment_sub-usa..."
  psql -h read-db -U admin -d PaymentDb -v ON_ERROR_STOP=1 -c "CREATE SUBSCRIPTION payment_sub-usa CONNECTION 'host=write-db port=5432 user=admin password=pass dbname=PaymentDb' PUBLICATION payment_pub;"
  


echo "Logical replication subscription created."
