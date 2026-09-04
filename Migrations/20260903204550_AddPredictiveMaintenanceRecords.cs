using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServerRoomMonitor.Migrations
{
    /// <inheritdoc />
    public partial class AddPredictiveMaintenanceRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PredictiveMaintenanceRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServerRoomId = table.Column<int>(type: "int", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Temperature = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    TemperatureDeviation = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    DaysSinceLastInspection = table.Column<int>(type: "int", nullable: false),
                    FailedInspectionsLast7Days = table.Column<int>(type: "int", nullable: false),
                    FailedInspectionsLast30Days = table.Column<int>(type: "int", nullable: false),
                    FailedAttemptsLast30Days = table.Column<int>(type: "int", nullable: false),
                    PreviousProblems = table.Column<int>(type: "int", nullable: false),
                    OverdueInspectionsLast30Days = table.Column<int>(type: "int", nullable: false),
                    DaysSinceLastRepair = table.Column<int>(type: "int", nullable: false),
                    AirConditioningOk = table.Column<bool>(type: "bit", nullable: false),
                    NoOverheatingAlarm = table.Column<bool>(type: "bit", nullable: false),
                    NoWaterLeak = table.Column<bool>(type: "bit", nullable: false),
                    PowerOk = table.Column<bool>(type: "bit", nullable: false),
                    RoomClean = table.Column<bool>(type: "bit", nullable: false),
                    FailureWithin7Days = table.Column<bool>(type: "bit", nullable: false),
                    IsSynthetic = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PredictiveMaintenanceRecords", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PredictiveMaintenanceRecords");
        }
    }
}
