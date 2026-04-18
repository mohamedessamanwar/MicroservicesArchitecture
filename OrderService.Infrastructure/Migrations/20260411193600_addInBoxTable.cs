using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addInBoxTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InboxMessages_Id",
                table: "InboxMessages");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "OutboxMessages");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "OutboxMessages",
                newName: "OccurredOnUtc");

            migrationBuilder.RenameColumn(
                name: "ProcessedAt",
                table: "InboxMessages",
                newName: "ProcessedOnUtc");

            migrationBuilder.AlterColumn<string>(
                name: "EventType",
                table: "OutboxMessages",
                type: "character varying(250)",
                maxLength: 250,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "ExchangeName",
                table: "OutboxMessages",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HeadersJson",
                table: "OutboxMessages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastError",
                table: "OutboxMessages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MessageId",
                table: "OutboxMessages",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessedOnUtc",
                table: "OutboxMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderName",
                table: "OutboxMessages",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                table: "OutboxMessages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RoutingKey",
                table: "OutboxMessages",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "OutboxMessages",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EventType",
                table: "InboxMessages",
                type: "character varying(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "MessageId",
                table: "InboxMessages",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Status_OccurredOnUtc",
                table: "OutboxMessages",
                columns: new[] { "Status", "OccurredOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_InboxMessages_MessageId",
                table: "InboxMessages",
                column: "MessageId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_Status_OccurredOnUtc",
                table: "OutboxMessages");

            migrationBuilder.DropIndex(
                name: "IX_InboxMessages_MessageId",
                table: "InboxMessages");

            migrationBuilder.DropColumn(
                name: "ExchangeName",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "HeadersJson",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "LastError",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "MessageId",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "ProcessedOnUtc",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "ProviderName",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "RoutingKey",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "EventType",
                table: "InboxMessages");

            migrationBuilder.DropColumn(
                name: "MessageId",
                table: "InboxMessages");

            migrationBuilder.RenameColumn(
                name: "OccurredOnUtc",
                table: "OutboxMessages",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "ProcessedOnUtc",
                table: "InboxMessages",
                newName: "ProcessedAt");

            migrationBuilder.AlterColumn<string>(
                name: "EventType",
                table: "OutboxMessages",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(250)",
                oldMaxLength: 250);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "OutboxMessages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_InboxMessages_Id",
                table: "InboxMessages",
                column: "Id",
                unique: true);
        }
    }
}
