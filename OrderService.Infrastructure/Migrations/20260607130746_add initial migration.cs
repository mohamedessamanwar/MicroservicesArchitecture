using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addinitialmigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    ProcessedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    ProviderName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ExchangeName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RoutingKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    HeadersJson = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    OccurredOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RuntimeMetricSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CapturedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CpuUsagePercent = table.Column<double>(type: "double precision", nullable: false),
                    CpuDeltaCpuMs = table.Column<double>(type: "double precision", nullable: false),
                    CpuDeltaWallMs = table.Column<double>(type: "double precision", nullable: false),
                    CpuLogicalProcessorCount = table.Column<int>(type: "integer", nullable: false),
                    RamWorkingSetMb = table.Column<double>(type: "double precision", nullable: false),
                    RamPrivateMemoryMb = table.Column<double>(type: "double precision", nullable: false),
                    RamManagedHeapMb = table.Column<double>(type: "double precision", nullable: false),
                    RamGcHeapMb = table.Column<double>(type: "double precision", nullable: false),
                    RamGcMemoryLoadMb = table.Column<double>(type: "double precision", nullable: false),
                    GcGen0Collections = table.Column<int>(type: "integer", nullable: false),
                    GcGen1Collections = table.Column<int>(type: "integer", nullable: false),
                    GcGen2Collections = table.Column<int>(type: "integer", nullable: false),
                    GcGen0Delta = table.Column<int>(type: "integer", nullable: false),
                    GcGen1Delta = table.Column<int>(type: "integer", nullable: false),
                    GcGen2Delta = table.Column<int>(type: "integer", nullable: false),
                    GcHeapSizeMb = table.Column<double>(type: "double precision", nullable: false),
                    GcMemoryLoadMb = table.Column<double>(type: "double precision", nullable: false),
                    GcTotalAvailableMemoryMb = table.Column<double>(type: "double precision", nullable: false),
                    GcHighMemoryLoadThresholdMb = table.Column<double>(type: "double precision", nullable: false),
                    GcFragmentedMb = table.Column<double>(type: "double precision", nullable: false),
                    ThreadPoolAvailableWorkerThreads = table.Column<int>(type: "integer", nullable: false),
                    ThreadPoolMaxWorkerThreads = table.Column<int>(type: "integer", nullable: false),
                    ThreadPoolMinWorkerThreads = table.Column<int>(type: "integer", nullable: false),
                    ThreadPoolAvailableIoCompletionThreads = table.Column<int>(type: "integer", nullable: false),
                    ThreadPoolMaxIoCompletionThreads = table.Column<int>(type: "integer", nullable: false),
                    ThreadPoolMinIoCompletionThreads = table.Column<int>(type: "integer", nullable: false),
                    ThreadPoolBusyWorkerThreads = table.Column<int>(type: "integer", nullable: false),
                    ProcessThreadCount = table.Column<int>(type: "integer", nullable: false),
                    SocketTotalConnections = table.Column<int>(type: "integer", nullable: true),
                    DbTotalConnections = table.Column<int>(type: "integer", nullable: true),
                    DbActiveConnections = table.Column<int>(type: "integer", nullable: true),
                    DbIdleConnections = table.Column<int>(type: "integer", nullable: true),
                    DbIdleInTransactionConnections = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuntimeMetricSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SpikeReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RuntimeMetricSnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    DetectedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CorrelationWindowStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CorrelationWindowEndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reasons = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpikeReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpikeReports_RuntimeMetricSnapshots_RuntimeMetricSnapshotId",
                        column: x => x.RuntimeMetricSnapshotId,
                        principalTable: "RuntimeMetricSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InboxMessages_MessageId",
                table: "InboxMessages",
                column: "MessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId",
                table: "Orders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status",
                table: "Orders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Status_OccurredOnUtc",
                table: "OutboxMessages",
                columns: new[] { "Status", "OccurredOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RuntimeMetricSnapshots_CapturedAtUtc",
                table: "RuntimeMetricSnapshots",
                column: "CapturedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SpikeReports_DetectedAtUtc",
                table: "SpikeReports",
                column: "DetectedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SpikeReports_RuntimeMetricSnapshotId",
                table: "SpikeReports",
                column: "RuntimeMetricSnapshotId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InboxMessages");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "SpikeReports");

            migrationBuilder.DropTable(
                name: "RuntimeMetricSnapshots");
        }
    }
}
