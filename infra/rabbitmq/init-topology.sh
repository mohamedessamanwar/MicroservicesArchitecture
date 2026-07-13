#!/bin/sh
set -e

RABBIT_HOST=${RABBITMQ_HOST:-rabbitmq}
RABBIT_PORT=${RABBITMQ_PORT:-15672}
RABBIT_USER=${RABBITMQ_USER:-admin}
RABBIT_PASS=${RABBITMQ_PASS:-admin123}

echo "Waiting for RabbitMQ Management API at ${RABBIT_HOST}:${RABBIT_PORT}..."
until rabbitmqadmin -H "${RABBIT_HOST}" -P "${RABBIT_PORT}" -u "${RABBIT_USER}" -p "${RABBIT_PASS}" list exchanges > /dev/null 2>&1; do
    echo "RabbitMQ not ready yet, sleeping 2s..."
    sleep 2
done

echo "RabbitMQ ready. Declaring CDC exchanges and queues..."

# CDC Exchange & Queues
rabbitmqadmin -H "${RABBIT_HOST}" -P "${RABBIT_PORT}" -u "${RABBIT_USER}" -p "${RABBIT_PASS}" declare exchange name=cdc.exchange type=topic durable=true
rabbitmqadmin -H "${RABBIT_HOST}" -P "${RABBIT_PORT}" -u "${RABBIT_USER}" -p "${RABBIT_PASS}" declare queue name=order.write.cdc.q durable=true
rabbitmqadmin -H "${RABBIT_HOST}" -P "${RABBIT_PORT}" -u "${RABBIT_USER}" -p "${RABBIT_PASS}" declare queue name=payment.write.cdc.q durable=true

rabbitmqadmin -H "${RABBIT_HOST}" -P "${RABBIT_PORT}" -u "${RABBIT_USER}" -p "${RABBIT_PASS}" declare binding source=cdc.exchange destination_type=queue destination=order.write.cdc.q routing_key="order-server.public.#"
rabbitmqadmin -H "${RABBIT_HOST}" -P "${RABBIT_PORT}" -u "${RABBIT_USER}" -p "${RABBIT_PASS}" declare binding source=cdc.exchange destination_type=queue destination=payment.write.cdc.q routing_key="payment-server.public.#"

echo "Declaring domain topic exchanges, queues, and bindings for Egypt and USA..."

# Shared Exchange
rabbitmqadmin -H "${RABBIT_HOST}" -P "${RABBIT_PORT}" -u "${RABBIT_USER}" -p "${RABBIT_PASS}" declare exchange name=order.exchange type=topic durable=true

# Egypt Domain


rabbitmqadmin -H "${RABBIT_HOST}" -P "${RABBIT_PORT}" -u "${RABBIT_USER}" -p "${RABBIT_PASS}" declare binding source=Egypt.order.exchange destination_type=queue destination=Egypt.order.Q routing_key="Egypt.order.#"
rabbitmqadmin -H "${RABBIT_HOST}" -P "${RABBIT_PORT}" -u "${RABBIT_USER}" -p "${RABBIT_PASS}" declare binding source=Egypt.order.exchange destination_type=queue destination=Egypt.order.Q routing_key="#"
rabbitmqadmin -H "${RABBIT_HOST}" -P "${RABBIT_PORT}" -u "${RABBIT_USER}" -p "${RABBIT_PASS}" declare binding source=order.exchange destination_type=queue destination=Egypt.order.Q routing_key="Egypt.order.#"



rabbitmqadmin -H "${RABBIT_HOST}" -P "${RABBIT_PORT}" -u "${RABBIT_USER}" -p "${RABBIT_PASS}" declare binding source=USA.order.exchange destination_type=queue destination=USA.order.Q routing_key="USA.order.#"
rabbitmqadmin -H "${RABBIT_HOST}" -P "${RABBIT_PORT}" -u "${RABBIT_USER}" -p "${RABBIT_PASS}" declare binding source=USA.order.exchange destination_type=queue destination=USA.order.Q routing_key="#"
rabbitmqadmin -H "${RABBIT_HOST}" -P "${RABBIT_PORT}" -u "${RABBIT_USER}" -p "${RABBIT_PASS}" declare binding source=order.exchange destination_type=queue destination=USA.order.Q routing_key="USA.order.#"

echo "RabbitMQ topology initialization complete!"
