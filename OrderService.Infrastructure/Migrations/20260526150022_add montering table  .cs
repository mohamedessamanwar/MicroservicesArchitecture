using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addmonteringtable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "SpikeReports");

            migrationBuilder.DropTable(
                name: "RuntimeMetricSnapshots");
        }
    }
}
