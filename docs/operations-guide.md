# Operations & Migration Guide

This document explains how to perform operational tasks on the refactored Docker setup, specifically regarding database migrations and physical replication.

## Connecting to PostgreSQL Manually

Since PostgreSQL does not expose port `5432` to the host directly anymore (for security reasons), you must connect to it from within the Docker network.

Use `docker compose exec` to drop into a temporary shell or execute commands directly.

```bash
# Connect to the primary write database
docker compose exec write-db psql -U admin -d write_db

# Connect to the read replica
docker compose exec read-db psql -U admin -d write_db
```

## Running Database Migrations

**Automatic migrations have been entirely disabled.**
You must run migrations manually against the primary database after verifying pending schema changes. 

In this .NET project, EF Core migration bundles are built into Docker images (`OrderService.Migrator.Dockerfile`, `PaymentService.Migrator.Dockerfile`), but they are not executed automatically.

To apply `.NET EF Core` migrations using a temporary administrative container:

```bash
# 1. Build the migration images
docker build -f docker/migrations/OrderService.Migrator.Dockerfile -t order-migrator .
docker build -f docker/migrations/PaymentService.Migrator.Dockerfile -t payment-migrator .

# 2. Run the migration container interactively (inside the docker network)
docker run --rm --network microservicesarchitecture_microservices-network -e "EF_CONNECTION_STRING=Host=write-db;Port=5432;Database=OrderDb;Username=admin;Password=change_me;" order-migrator
```

*Note: For projects using Laravel or Yii2 (as referenced by policy), the equivalent commands would be:*
```bash
# Laravel Example
docker compose exec laravel-php php artisan migrate

# Yii2 Example
docker compose exec yii-php php yii migrate
```

## PostgreSQL Physical Standby Rebuild Guide

If the replica database gets out of sync, corrupted, or needs to be rebuilt, you can safely wipe its data and allow `read-startup.sh` to initialize it again via `pg_basebackup`.

1. Stop the read replica:
   ```bash
   docker compose stop read-db
   ```
2. Remove the replica container:
   ```bash
   docker compose rm -f read-db
   ```
3. Destroy the replica volume:
   ```bash
   docker volume rm microservicesarchitecture_read_db_data
   ```
4. Start the replica again:
   ```bash
   docker compose up -d read-db
   ```

The script `read-startup.sh` will detect that the data directory is empty and will invoke `pg_basebackup` to fetch a fresh snapshot from `write-db`.

## Verification After Deployment

Run these safe checks before and after deployment:

```bash
# Validate Compose file syntax
docker compose config

# Check running containers and health checks
docker compose ps

# Check resources and bounds
docker stats
```
