using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServerRoomMonitor.Migrations
{
    /// <inheritdoc />
    public partial class AddTechnicianToInspection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "Inspections",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TechnicianId",
                table: "Inspections",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Inspections_TechnicianId",
                table: "Inspections",
                column: "TechnicianId");

            migrationBuilder.AddForeignKey(
                name: "FK_Inspections_AspNetUsers_TechnicianId",
                table: "Inspections",
                column: "TechnicianId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Inspections_AspNetUsers_TechnicianId",
                table: "Inspections");

            migrationBuilder.DropIndex(
                name: "IX_Inspections_TechnicianId",
                table: "Inspections");

            migrationBuilder.DropColumn(
                name: "TechnicianId",
                table: "Inspections");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "Inspections",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
