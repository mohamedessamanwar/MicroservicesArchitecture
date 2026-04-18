CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

CREATE TABLE "InboxMessages" (
    "Id" uuid NOT NULL,
    "ProcessedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_InboxMessages" PRIMARY KEY ("Id")
);

CREATE TABLE "Orders" (
    "Id" uuid NOT NULL,
    "CustomerId" uuid NOT NULL,
    "TotalAmount" numeric(18,2) NOT NULL,
    "Status" integer NOT NULL,
    "Created" timestamp with time zone,
    "Modified" timestamp with time zone,
    CONSTRAINT "PK_Orders" PRIMARY KEY ("Id")
);

CREATE TABLE "OutboxMessages" (
    "Id" uuid NOT NULL,
    "EventType" text NOT NULL,
    "Payload" text NOT NULL,
    "IsPublished" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_OutboxMessages" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "IX_InboxMessages_Id" ON "InboxMessages" ("Id");

CREATE INDEX "IX_Orders_CustomerId" ON "Orders" ("CustomerId");

CREATE INDEX "IX_Orders_Status" ON "Orders" ("Status");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260407202336_addDb', '8.0.0');

COMMIT;

