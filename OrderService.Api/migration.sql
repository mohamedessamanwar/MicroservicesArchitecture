CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260607130746_add initial migration') THEN
    CREATE TABLE "InboxMessages" (
        "Id" uuid NOT NULL,
        "MessageId" uuid NOT NULL,
        "EventType" character varying(250) NOT NULL,
        "ProcessedOnUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_InboxMessages" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260607130746_add initial migration') THEN
    CREATE TABLE "Orders" (
        "Id" uuid NOT NULL,
        "CustomerId" uuid NOT NULL,
        "TotalAmount" numeric(18,2) NOT NULL,
        "Status" integer NOT NULL,
        "Created" timestamp with time zone,
        "Modified" timestamp with time zone,
        CONSTRAINT "PK_Orders" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260607130746_add initial migration') THEN
    CREATE TABLE "OutboxMessages" (
        "Id" uuid NOT NULL,
        "MessageId" uuid NOT NULL,
        "EventType" character varying(250) NOT NULL,
        "Payload" text NOT NULL,
        "ProviderName" character varying(120) NOT NULL,
        "ExchangeName" character varying(200) NOT NULL,
        "RoutingKey" character varying(200) NOT NULL,
        "HeadersJson" text,
        "Status" character varying(40) NOT NULL,
        "RetryCount" integer NOT NULL,
        "OccurredOnUtc" timestamp with time zone NOT NULL,
        "ProcessedOnUtc" timestamp with time zone,
        "LastError" text,
        CONSTRAINT "PK_OutboxMessages" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260607130746_add initial migration') THEN
    CREATE TABLE "RuntimeMetricSnapshots" (
        "Id" uuid NOT NULL,
        "CapturedAtUtc" timestamp with time zone NOT NULL,
        "CpuUsagePercent" double precision NOT NULL,
        "CpuDeltaCpuMs" double precision NOT NULL,
        "CpuDeltaWallMs" double precision NOT NULL,
        "CpuLogicalProcessorCount" integer NOT NULL,
        "RamWorkingSetMb" double precision NOT NULL,
        "RamPrivateMemoryMb" double precision NOT NULL,
        "RamManagedHeapMb" double precision NOT NULL,
        "RamGcHeapMb" double precision NOT NULL,
        "RamGcMemoryLoadMb" double precision NOT NULL,
        "GcGen0Collections" integer NOT NULL,
        "GcGen1Collections" integer NOT NULL,
        "GcGen2Collections" integer NOT NULL,
        "GcGen0Delta" integer NOT NULL,
        "GcGen1Delta" integer NOT NULL,
        "GcGen2Delta" integer NOT NULL,
        "GcHeapSizeMb" double precision NOT NULL,
        "GcMemoryLoadMb" double precision NOT NULL,
        "GcTotalAvailableMemoryMb" double precision NOT NULL,
        "GcHighMemoryLoadThresholdMb" double precision NOT NULL,
        "GcFragmentedMb" double precision NOT NULL,
        "ThreadPoolAvailableWorkerThreads" integer NOT NULL,
        "ThreadPoolMaxWorkerThreads" integer NOT NULL,
        "ThreadPoolMinWorkerThreads" integer NOT NULL,
        "ThreadPoolAvailableIoCompletionThreads" integer NOT NULL,
        "ThreadPoolMaxIoCompletionThreads" integer NOT NULL,
        "ThreadPoolMinIoCompletionThreads" integer NOT NULL,
        "ThreadPoolBusyWorkerThreads" integer NOT NULL,
        "ProcessThreadCount" integer NOT NULL,
        "SocketTotalConnections" integer,
        "DbTotalConnections" integer,
        "DbActiveConnections" integer,
        "DbIdleConnections" integer,
        "DbIdleInTransactionConnections" integer,
        CONSTRAINT "PK_RuntimeMetricSnapshots" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260607130746_add initial migration') THEN
    CREATE TABLE "SpikeReports" (
        "Id" uuid NOT NULL,
        "RuntimeMetricSnapshotId" uuid NOT NULL,
        "DetectedAtUtc" timestamp with time zone NOT NULL,
        "CorrelationWindowStartUtc" timestamp with time zone NOT NULL,
        "CorrelationWindowEndUtc" timestamp with time zone NOT NULL,
        "Reasons" text NOT NULL,
        CONSTRAINT "PK_SpikeReports" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_SpikeReports_RuntimeMetricSnapshots_RuntimeMetricSnapshotId" FOREIGN KEY ("RuntimeMetricSnapshotId") REFERENCES "RuntimeMetricSnapshots" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260607130746_add initial migration') THEN
    CREATE UNIQUE INDEX "IX_InboxMessages_MessageId" ON "InboxMessages" ("MessageId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260607130746_add initial migration') THEN
    CREATE INDEX "IX_Orders_CustomerId" ON "Orders" ("CustomerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260607130746_add initial migration') THEN
    CREATE INDEX "IX_Orders_Status" ON "Orders" ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260607130746_add initial migration') THEN
    CREATE INDEX "IX_OutboxMessages_Status_OccurredOnUtc" ON "OutboxMessages" ("Status", "OccurredOnUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260607130746_add initial migration') THEN
    CREATE INDEX "IX_RuntimeMetricSnapshots_CapturedAtUtc" ON "RuntimeMetricSnapshots" ("CapturedAtUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260607130746_add initial migration') THEN
    CREATE INDEX "IX_SpikeReports_DetectedAtUtc" ON "SpikeReports" ("DetectedAtUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260607130746_add initial migration') THEN
    CREATE INDEX "IX_SpikeReports_RuntimeMetricSnapshotId" ON "SpikeReports" ("RuntimeMetricSnapshotId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260607130746_add initial migration') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260607130746_add initial migration', '8.0.0');
    END IF;
END $EF$;
COMMIT;

