# PostgreSQL Logical Replication, Slots, and Debezium Guide

This document explains the core concepts of PostgreSQL replication slots, logical replication, and how Debezium interacts with them, including best practices for configuration.

## 1. What is a Replication Slot?

A **Replication Slot** is a mechanism in PostgreSQL that ensures the database server retains the Write Ahead Log (WAL) segments needed by standby servers or logical decoding clients (like Debezium) until they have confirmed receipt.

### Why do we need it?

PostgreSQL maintains a transaction log (WAL) of all changes. Normally, these logs are deleted or recycled once they are written to disk to save space. However, a consumer (like a replica or Debezium) might trail behind the main database.

- **Without a slot**: If the consumer disconnects or falls behind, the standard PostgreSQL cleanup process might delete the WAL files the consumer still needs. This forces the consumer to restart from a full snapshot.
- **With a slot**: PostgreSQL guarantees it will **not delete** any WAL data that is newer than the oldest confirmed position of the slot.

### Types of Slots

1.  **Physical Slots**: Used for physical streaming replication (whole database mirroring).
2.  **Logical Slots**: Used for logical decoding (streaming changes of specific tables/databases in a generic format like JSON, Protobuf, or Avro). This is what **Debezium** uses.

---

## 2. technically Creating a Logical Replica

To set up a logical replica (Database A replicating data to Database B), follow these steps.

### Step 1: Configuration (Publisher & Subscriber)

Ensure `postgresql.conf` has the following setting (requires restart):

```ini
wal_level = logical
```

### Step 2: Setup Publisher (Source DB)

Connect to the source database and create a publication. You can select specific tables or "ALL TABLES".

```sql
-- Create a publication for all tables
CREATE PUBLICATION my_publication FOR ALL TABLES;

-- OR for specific tables
CREATE PUBLICATION my_publication FOR TABLE orders, customers;
```

### Step 3: Setup Subscriber (Destination DB)

Connect to the destination database and create a subscription. This command automatically creates a replication slot on the Publisher.

```sql
CREATE SUBSCRIPTION my_subscription
CONNECTION 'host=source_db_host port=5432 dbname=source_db user=admin password=secret'
PUBLICATION my_publication;
```

_Note: The table schemas (columns, types) must exist on the subscriber before creating the subscription._

---

## 3. How Debezium Works

Debezium is a **Change Data Capture (CDC)** tool that acts as a client reading the PostgreSQL transaction log.

### The Mechanics

1.  **Snapshotting** (Initial Sync): When Debezium starts for the first time, it (optionally) takes a consistent snapshot of the tables to capture the current state.
2.  **Streaming**:
    - Debezium connects to PostgreSQL using the replication protocol.
    - It creates (or uses an existing) **Logical Replication Slot**.
    - It utilizes a **Logical Decoding Plugin** (usually `pgoutput`, the standard in PG 10+) to convert WAL entries into a stream of events.
    - PostgreSQL continuously sends these change events to Debezium.
    - Debezium commits the **Log Sequence Number (LSN)** (the offset) back to PostgreSQL periodically. "I have read up to point X".
    - PostgreSQL can then safely delete WAL logs older than point X.

---

## 4. Best Practices: Slots and Debezium

### Can Debezium and a Logical Replica use the same Generic Slot?

**NO.**

### Why?

A replication slot tracks the **confirmed flush LSN** (the position in the WAL log).

- If **Consumer A** (Debezium) reads a change, the slot updates its position.
- If **Consumer B** (Replica) tries to read the same slot, it will see that the data has already been "consumed" and will receive nothing (or they will fight over the connection, as PG usually allows only one active consumer per slot).

### Technical Consequence

If you try to point two tools at the same slot:

1.  **Connection Conflicts**: PostgreSQL generally locks the slot to the active PID. The second connection attempts often fail or block.
2.  **Data Loss**: Even if they could share, whoever acknowledges the data first "removes" it from the pending queue for that slot. The other consumer would miss those events.

### The Best Practice Strategy

**Always use separate slots for separate consumers.**

1.  **Slot for Replica**: Let the PostgreSQL `CREATE SUBSCRIPTION` command manage its own slot (e.g., `my_subscription` slot).
2.  **Slot for Debezium**: Configure Debezium to use its own distinct slot name (e.g., `debezium_orders_slot`).

This ensures that Debezium can process events at its own pace (sending to Kafka/RabbitMQ) while the Logical Replica processes the same events at its own pace (writing to the DB) without interfering with each other. Both will hold onto the necessary WAL logs until _both_ have finished processing them.

---

## 5. Current Project Configuration

In this Microservices Architecture project, we strictly follow the best practice of using separate slots.

### 1. Debezium Slot (`order_slot`)

Debezium is explicitly configured to use a unique slot name.

- **File**: `DockerCompose.yaml` (Debezium Service) & `debezium/orderdb-application.properties`
- **Config**: `DEBEZIUM_SOURCE_SLOT_NAME=order_slot`
- **Purpose**: Tracks Debezium's progress in streaming changes to RabbitMQ.

### 2. Logical Replica Slot (`order_sub`)

The Asynchronous Read Replica uses a standard PostgreSQL subscription.

- **File**: `DockerCompose.yaml` (`replica-setup` service command)
- **Command**: `CREATE SUBSCRIPTION order_sub ...`
- **Purpose**: PostgreSQL automatically creates a slot named `order_sub` to track the replication progress from the Write DB to the Read DB.

### 3. Shared Publication (`order_pub`)

Both consumers share the same Publication, which is efficient and correct.

- **File**: `DockerCompose.yaml` (`db-init` service)
- **Command**: `CREATE PUBLICATION order_pub FOR ALL TABLES;`
- **Purpose**: Defines _what_ data is available for replication (all tables), but does not track _who_ has received it. That is the job of the slots.
