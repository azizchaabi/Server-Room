using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServerRoomMonitor.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduledInspections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScheduledInspections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServerRoomId = table.Column<int>(type: "int", nullable: false),
                    TechnicianId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ScheduledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledInspections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduledInspections_AspNetUsers_TechnicianId",
                        column: x => x.TechnicianId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ScheduledInspections_ServerRooms_ServerRoomId",
                        column: x => x.ServerRoomId,
                        principalTable: "ServerRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledInspections_ServerRoomId",
                table: "ScheduledInspections",
                column: "ServerRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledInspections_TechnicianId",
                table: "ScheduledInspections",
                column: "TechnicianId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScheduledInspections");
        }
    }
}
