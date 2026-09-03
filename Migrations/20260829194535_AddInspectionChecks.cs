using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServerRoomMonitor.Migrations
{
    /// <inheritdoc />
    public partial class AddInspectionChecks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AirConditioningOk",
                table: "Inspections",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NoOverheatingAlarm",
                table: "Inspections",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NoWaterLeak",
                table: "Inspections",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Inspections",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "PowerOk",
                table: "Inspections",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RoomClean",
                table: "Inspections",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AirConditioningOk",
                table: "Inspections");

            migrationBuilder.DropColumn(
                name: "NoOverheatingAlarm",
                table: "Inspections");

            migrationBuilder.DropColumn(
                name: "NoWaterLeak",
                table: "Inspections");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Inspections");

            migrationBuilder.DropColumn(
                name: "PowerOk",
                table: "Inspections");

            migrationBuilder.DropColumn(
                name: "RoomClean",
                table: "Inspections");
        }
    }
}
