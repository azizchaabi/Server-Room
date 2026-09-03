using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServerRoomMonitor.Migrations
{
    /// <inheritdoc />
    public partial class UpdateInspectionChecks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Check1Ok",
                table: "Inspections");

            migrationBuilder.RenameColumn(
                name: "Check2Ok",
                table: "Inspections",
                newName: "TemperatureOk");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TemperatureOk",
                table: "Inspections",
                newName: "Check2Ok");

            migrationBuilder.AddColumn<bool>(
                name: "Check1Ok",
                table: "Inspections",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
