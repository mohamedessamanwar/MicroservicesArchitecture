#!/bin/sh
set -e

RABBIT_HOST=${RABBITMQ_HOST:-rabbitmq}
RABBIT_PORT=${RABBITMQ_PORT:-15672}
RABBIT_USER=${RABBITMQ_USER:-${RABBITMQ_DEFAULT_USER:-admin}}
RABBIT_PASS=${RABBITMQ_PASS:-${RABBITMQ_DEFAULT_PASS:-admin123}}

echo "Waiting for RabbitMQ Management API at ${RABBIT_HOST}:${RABBIT_PORT}..."
until rabbitmqadmin -H "${RABBIT_HOST}" -P "${RABBIT_PORT}" -u "${RABBIT_USER}" -p "${RABBIT_PASS}" list exchanges > /dev/null 2>&1; do
    echo "RabbitMQ not ready yet, sleeping 2s..."
    sleep 2
done

echo "RabbitMQ ready. Declaring domain topic exchanges, queues, and bindings for Egypt and USA..."

# Shared Exchange
rabbitmqadmin -H "${RABBIT_HOST}" -P "${RABBIT_PORT}" -u "${RABBIT_USER}" -p "${RABBIT_PASS}" declare exchange name=order.exchange type=topic durable=true

# Egypt Domain
# Egypt DLX & DLQ
rabbitmqadmin -H "${RABBIT_HOST}" -P "${RABBIT_PORT}" -u "${RABBIT_USER}" -p "${RABBIT_PASS}" declare exchange name=Egypt.order.Q.dead-letter-exchange type=direct durable=true
rabbitmqadmin -H "${RABBIT_HOST}" -P "${RABBIT_PORT}" -u "${RABBIT_USER}" -p "${RABBIT_PASS}" declare queue name=Egypt.order.Q.dlq durable=true
rabbitmqadmin -H "${RABBIT_HOST}" -P "${RABBIT_PORT}" -u "${RABBIT_USER}" -p "${RABBIT_PASS}" declare binding source=Egypt.order.Q.dead-letter-exchange destination_type=queue destination=Egypt.order.Q.dlq routing_key=Egypt.order.Q.dlq

# Egypt Main Components
rabbitmqadmin -H "${RABBIT_HOST}" -P "${RABBIT_PORT}" -u "${RABBIT_USER}" -p "${RABBIT_PASS}" declare exchange name=Egypt.order.exchange type=topic durable=true
rabbitmqadmin -H "${RABBIT_HOST}" -P "${RABBIT_PORT}" -u "${RABBIT_USER}" -p "${RABBIT_PASS}" declare queue name=Egypt.order.Q durable=true arguments='{"x-dead-letter-exchange": "Egypt.order.Q.dead-letter-exchange", "x-dead-letter-routing-key": "Egypt.order.Q.dlq"}'

rabbitmqadmin -H "${RABBIT_HOST}" -P "${RABBIT_PORT}" -u "${RABBIT_USER}" -p "${RABBIT_PASS}" declare binding source=Egypt.order.exchange destination_type=queue destination=Egypt.order.Q routing_key="Egypt.order.#"
rabbitmqadmin -H "${RABBIT_HOST}" -P "${RABBIT_PORT}" -u "${RABBIT_USER}" -p "${RABBIT_PASS}" declare binding source=Egypt.order.exchange destination_type=queue destination=Egypt.order.Q routing_key="#"
rabbitmqadmin -H "${RABBIT_HOST}" -P "${RABBIT_PORT}" -u "${RABBIT_USER}" -p "${RABBIT_PASS}" declare binding source=order.exchange destination_type=queue destination=Egypt.order.Q routing_key="Egypt.order.#"

# USA Domain
# USA DLX & DLQ
rabbitmqadmin -H "${RABBIT_HOST}" -P "${RABBIT_PORT}" -u "${RABBIT_USER}" -p "${RABBIT_PASS}" declare exchange name=USA.order.Q.dead-letter-exchange type=direct durable=true
rabbitmqadmin -H "${RABBIT_HOST}" -P "${RABBIT_PORT}" -u "${RABBIT_USER}" -p "${RABBIT_PASS}" declare queue name=USA.order.Q.dlq durable=true
rabbitmqadmin -H "${RABBIT_HOST}" -P "${RABBIT_PORT}" -u "${RABBIT_USER}" -p "${RABBIT_PASS}" declare binding source=USA.order.Q.dead-letter-exchange destination_type=queue destination=USA.order.Q.dlq routing_key=USA.order.Q.dlq

# USA Main Components
rabbitmqadmin -H "${RABBIT_HOST}" -P "${RABBIT_PORT}" -u "${RABBIT_USER}" -p "${RABBIT_PASS}" declare exchange name=USA.order.exchange type=topic durable=true
rabbitmqadmin -H "${RABBIT_HOST}" -P "${RABBIT_PORT}" -u "${RABBIT_USER}" -p "${RABBIT_PASS}" declare queue name=USA.order.Q durable=true arguments='{"x-dead-letter-exchange": "USA.order.Q.dead-letter-exchange", "x-dead-letter-routing-key": "USA.order.Q.dlq"}'

rabbitmqadmin -H "${RABBIT_HOST}" -P "${RABBIT_PORT}" -u "${RABBIT_USER}" -p "${RABBIT_PASS}" declare binding source=USA.order.exchange destination_type=queue destination=USA.order.Q routing_key="USA.order.#"
rabbitmqadmin -H "${RABBIT_HOST}" -P "${RABBIT_PORT}" -u "${RABBIT_USER}" -p "${RABBIT_PASS}" declare binding source=USA.order.exchange destination_type=queue destination=USA.order.Q routing_key="#"
rabbitmqadmin -H "${RABBIT_HOST}" -P "${RABBIT_PORT}" -u "${RABBIT_USER}" -p "${RABBIT_PASS}" declare binding source=order.exchange destination_type=queue destination=USA.order.Q routing_key="USA.order.#"

echo "RabbitMQ topology initialization complete!"
