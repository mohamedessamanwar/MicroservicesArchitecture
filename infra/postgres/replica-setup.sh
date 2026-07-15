#!/usr/bin/env sh
set -eu

create_subscription() {
  local subname=$1
  local dbname=$2
  local pubname=$3

  echo "Checking for subscription '${subname}' on database '${dbname}'..."
  if psql -h read-db -U admin -d "${dbname}" -Atqc "SELECT 1 FROM pg_subscription WHERE subname = '${subname}';" | grep -q 1; then
    echo "Subscription ${subname} already exists. Skipping."
    return 0
  fi
  
  echo "Cleaning up potential stale slots on primary for ${subname}..."
  psql -h write-db -U admin -d "${dbname}" -v ON_ERROR_STOP=1 -Atqc "SELECT pg_drop_replication_slot('${subname}') WHERE EXISTS (SELECT 1 FROM pg_replication_slots WHERE slot_name = '${subname}');" || true

  echo "Creating subscription ${subname} on ${dbname}..."
  psql -h read-db -U admin -d "${dbname}" -v ON_ERROR_STOP=1 -c "CREATE SUBSCRIPTION \"${subname}\" CONNECTION 'host=write-db port=5432 user=admin password=pass dbname=${dbname}' PUBLICATION ${pubname};"
}

create_subscription "order_sub" "OrderDb" "order_pub"
create_subscription "payment_sub" "PaymentDb" "payment_pub"
create_subscription "order_sub_usa" "OrderDb-USA" "order_pub_usa"
create_subscription "payment_sub_usa" "PaymentDb-USA" "payment_pub_usa"


echo "Logical replication subscriptions created successfully."
