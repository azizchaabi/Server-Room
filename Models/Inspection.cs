using Microsoft.AspNetCore.Identity;

namespace ServerRoomMonitor.Models;

public class Inspection
{
    public int Id { get; set; }

    public int ServerRoomId { get; set; }

    public ServerRoom? ServerRoom { get; set; }

    // The scheduled inspection that authorized this inspection
    public int? ScheduledInspectionId { get; set; }

    public ScheduledInspection? ScheduledInspection { get; set; }

    // Technician who performed the inspection
    public string? TechnicianId { get; set; } = "";

    public IdentityUser? Technician { get; set; }

    // When the inspection was actually performed
    public DateTime CheckedAt { get; set; }

    // Which attempt this was
    // 1 = first attempt
    // 2 = second attempt
    // 3 = third attempt
    public int AttemptNumber { get; set; }

    // Temperature
    public decimal Temperature { get; set; }

    public bool TemperatureOk { get; set; }

    // Physical / equipment checks
    public bool AirConditioningOk { get; set; }

    public bool NoOverheatingAlarm { get; set; }

    public bool NoWaterLeak { get; set; }

    public bool PowerOk { get; set; }

    public bool RoomClean { get; set; }

    // Technician notes
    public string? Notes { get; set; } = "";

    // Overall inspection result
    public bool IsOk { get; set; }
}