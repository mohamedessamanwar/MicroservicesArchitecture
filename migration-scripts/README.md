# EF Core Migration Bundles Architecture

Static SQL migration scripts (`order-migration.sql`, `payment-migration.sql`, `migration.sql`) are **no longer needed or stored** in this repository (`write-startup.sh` / `read-startup.sh` do not execute any SQL migration scripts).

## Why Were Static SQL Scripts Removed?
Previously, raw SQL scripts mounted into `/scripts` were executed directly by PostgreSQL startup scripts when the database container launched. This approach has been replaced with **EF Core Migration Bundle Containers** to ensure:
1. Exact source-of-truth alignment with compiled C# EF Core migration files (`OrderService.Infrastructure/Persistence/Migrations` and `Payment.Infrastructure/Persistence/Migrations`).
2. Reliable, isolated, one-time application of pending migrations before application services launch.
3. Strict separation of concerns between database infrastructure initialization (`write-startup.sh` / `read-startup.sh`) and application schema evolution.

## Current Migration Execution Mechanism
In `docker-compose.yml`, schema migrations are executed by dedicated, short-lived Docker containers (`order-migration-egypt`, `order-migration-usa`, `payment-migration-egypt`, `payment-migration-usa`, etc.) built using `docker/migrations/OrderService.Migrator.Dockerfile` and `docker/migrations/PaymentService.Migrator.Dockerfile`.
