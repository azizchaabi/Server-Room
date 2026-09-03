using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServerRoomMonitor.Migrations
{
    /// <inheritdoc />
    public partial class AddInspectionWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScheduledInspections_AspNetUsers_TechnicianId",
                table: "ScheduledInspections");

            migrationBuilder.DropForeignKey(
                name: "FK_ScheduledInspections_ServerRooms_ServerRoomId",
                table: "ScheduledInspections");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "ServerRooms",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "AttemptCount",
                table: "ScheduledInspections",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "Deadline",
                table: "ScheduledInspections",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "FixedAt",
                table: "ScheduledInspections",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FixedByAdminId",
                table: "ScheduledInspections",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Temperature",
                table: "Inspections",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddColumn<int>(
                name: "AttemptNumber",
                table: "Inspections",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ScheduledInspectionId",
                table: "Inspections",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledInspections_FixedByAdminId",
                table: "ScheduledInspections",
                column: "FixedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_Inspections_ScheduledInspectionId",
                table: "Inspections",
                column: "ScheduledInspectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Inspections_ScheduledInspections_ScheduledInspectionId",
                table: "Inspections",
                column: "ScheduledInspectionId",
                principalTable: "ScheduledInspections",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduledInspections_AspNetUsers_FixedByAdminId",
                table: "ScheduledInspections",
                column: "FixedByAdminId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduledInspections_AspNetUsers_TechnicianId",
                table: "ScheduledInspections",
                column: "TechnicianId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduledInspections_ServerRooms_ServerRoomId",
                table: "ScheduledInspections",
                column: "ServerRoomId",
                principalTable: "ServerRooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Inspections_ScheduledInspections_ScheduledInspectionId",
                table: "Inspections");

            migrationBuilder.DropForeignKey(
                name: "FK_ScheduledInspections_AspNetUsers_FixedByAdminId",
                table: "ScheduledInspections");

            migrationBuilder.DropForeignKey(
                name: "FK_ScheduledInspections_AspNetUsers_TechnicianId",
                table: "ScheduledInspections");

            migrationBuilder.DropForeignKey(
                name: "FK_ScheduledInspections_ServerRooms_ServerRoomId",
                table: "ScheduledInspections");

            migrationBuilder.DropIndex(
                name: "IX_ScheduledInspections_FixedByAdminId",
                table: "ScheduledInspections");

            migrationBuilder.DropIndex(
                name: "IX_Inspections_ScheduledInspectionId",
                table: "Inspections");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ServerRooms");

            migrationBuilder.DropColumn(
                name: "AttemptCount",
                table: "ScheduledInspections");

            migrationBuilder.DropColumn(
                name: "Deadline",
                table: "ScheduledInspections");

            migrationBuilder.DropColumn(
                name: "FixedAt",
                table: "ScheduledInspections");

            migrationBuilder.DropColumn(
                name: "FixedByAdminId",
                table: "ScheduledInspections");

            migrationBuilder.DropColumn(
                name: "AttemptNumber",
                table: "Inspections");

            migrationBuilder.DropColumn(
                name: "ScheduledInspectionId",
                table: "Inspections");

            migrationBuilder.AlterColumn<decimal>(
                name: "Temperature",
                table: "Inspections",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduledInspections_AspNetUsers_TechnicianId",
                table: "ScheduledInspections",
                column: "TechnicianId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduledInspections_ServerRooms_ServerRoomId",
                table: "ScheduledInspections",
                column: "ServerRoomId",
                principalTable: "ServerRooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
