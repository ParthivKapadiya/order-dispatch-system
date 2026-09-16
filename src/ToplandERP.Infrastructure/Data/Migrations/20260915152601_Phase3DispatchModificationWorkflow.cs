using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToplandERP.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase3DispatchModificationWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Dispatches_OrderId",
                table: "Dispatches");

            migrationBuilder.AddColumn<string>(
                name: "CurrentSnapshotJson",
                table: "OrderModificationRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "OrderModificationRequests",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestedByName",
                table: "OrderModificationRequests",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RequestedChangesJson",
                table: "OrderModificationRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "OrderModificationRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewedByName",
                table: "OrderModificationRequests",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedByUserId",
                table: "OrderModificationRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BookingNumber",
                table: "Dispatches",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DispatchDate",
                table: "Dispatches",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DispatchPersonName",
                table: "Dispatches",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "DispatchedByUserId",
                table: "Dispatches",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                table: "Dispatches",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LrNumber",
                table: "Dispatches",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DocumentType",
                table: "DispatchDocuments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UploadedByUserId",
                table: "DispatchDocuments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderModificationRequests_OrderId_Status",
                table: "OrderModificationRequests",
                columns: new[] { "OrderId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Dispatches_OrderId",
                table: "Dispatches",
                column: "OrderId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrderModificationRequests_OrderId_Status",
                table: "OrderModificationRequests");

            migrationBuilder.DropIndex(
                name: "IX_Dispatches_OrderId",
                table: "Dispatches");

            migrationBuilder.DropColumn(
                name: "CurrentSnapshotJson",
                table: "OrderModificationRequests");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "OrderModificationRequests");

            migrationBuilder.DropColumn(
                name: "RequestedByName",
                table: "OrderModificationRequests");

            migrationBuilder.DropColumn(
                name: "RequestedChangesJson",
                table: "OrderModificationRequests");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "OrderModificationRequests");

            migrationBuilder.DropColumn(
                name: "ReviewedByName",
                table: "OrderModificationRequests");

            migrationBuilder.DropColumn(
                name: "ReviewedByUserId",
                table: "OrderModificationRequests");

            migrationBuilder.DropColumn(
                name: "BookingNumber",
                table: "Dispatches");

            migrationBuilder.DropColumn(
                name: "DispatchDate",
                table: "Dispatches");

            migrationBuilder.DropColumn(
                name: "DispatchPersonName",
                table: "Dispatches");

            migrationBuilder.DropColumn(
                name: "DispatchedByUserId",
                table: "Dispatches");

            migrationBuilder.DropColumn(
                name: "IsCompleted",
                table: "Dispatches");

            migrationBuilder.DropColumn(
                name: "LrNumber",
                table: "Dispatches");

            migrationBuilder.DropColumn(
                name: "UploadedByUserId",
                table: "DispatchDocuments");

            migrationBuilder.AlterColumn<string>(
                name: "DocumentType",
                table: "DispatchDocuments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.CreateIndex(
                name: "IX_Dispatches_OrderId",
                table: "Dispatches",
                column: "OrderId");
        }
    }
}
