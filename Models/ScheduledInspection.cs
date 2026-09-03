using Microsoft.AspNetCore.Identity;

namespace ServerRoomMonitor.Models;

public class ScheduledInspection
{
    public int Id { get; set; }

    // Server room being inspected
    public int ServerRoomId { get; set; }

    public ServerRoom? ServerRoom { get; set; }

    // Technician assigned by the administrator
    public string? TechnicianId { get; set; }

    public IdentityUser? Technician { get; set; }

    // When the inspection becomes available
    public DateTime ScheduledAt { get; set; }

    // Deadline for completing the inspection
    public DateTime Deadline { get; set; }

    // Scheduled / In Progress / Completed /
    // Cancelled / Overdue / Requires Fix /
    // Awaiting Verification
    public string Status { get; set; } = "Scheduled";

    // Number of attempts already completed
    public int AttemptCount { get; set; } = 0;

    // Optional administrator notes
    public string? Notes { get; set; } = "";

    // When this appointment was created
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // When an administrator declared the issue fixed
    public DateTime? FixedAt { get; set; }

    // Administrator who declared it fixed
    public string? FixedByAdminId { get; set; }

    public IdentityUser? FixedByAdmin { get; set; }

    // Actual inspection results belonging to this schedule
    public List<Inspection> Inspections { get; set; } = new();
}