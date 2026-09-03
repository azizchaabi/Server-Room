using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ServerRoomMonitor.Data;
using ServerRoomMonitor.Models;

namespace ServerRoomMonitor.Pages.Admin.Calendar;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        return Page();
    }

    public async Task<IActionResult> OnGetEventsAsync()
    {
        var inspections = await _context.ScheduledInspections
            .Include(s => s.ServerRoom)
            .Include(s => s.Technician)
            .OrderBy(s => s.ScheduledAt)
            .ToListAsync();

        var events = inspections.Select(s => new
        {
            id = s.Id.ToString(),

            title = $"{s.ServerRoom?.Name ?? "Unknown Room"} - {GetTechnicianName(s.Technician)}",

            start = s.ScheduledAt.ToString("yyyy-MM-ddTHH:mm:ss"),

            end = s.Deadline.ToString("yyyy-MM-ddTHH:mm:ss"),

            backgroundColor = GetStatusColor(s.Status),

            borderColor = GetStatusColor(s.Status),

            textColor = "#ffffff",

            extendedProps = new
            {
                roomName = s.ServerRoom?.Name ?? "Unknown Room",

                technician = GetTechnicianName(s.Technician),

                scheduledAt = s.ScheduledAt.ToString("dd/MM/yyyy HH:mm"),

                deadline = s.Deadline.ToString("dd/MM/yyyy HH:mm"),

                status = s.Status,

                attempts = s.AttemptCount,

                notes = s.Notes ?? ""
            }
        });

        return new JsonResult(events);
    }

    private static string GetTechnicianName(IdentityUser? technician)
    {
        if (technician == null)
            return "Unassigned";

        if (!string.IsNullOrWhiteSpace(technician.UserName))
            return technician.UserName;

        if (!string.IsNullOrWhiteSpace(technician.Email))
            return technician.Email;

        return "Unknown technician";
    }

    private static string GetStatusColor(string? status)
    {
        return status switch
        {
            "Scheduled" => "#2563eb",

            "In Progress" => "#f59e0b",

            "Awaiting Verification" => "#7c3aed",

            "Completed" => "#16a34a",

            "Requires Fix" => "#dc2626",

            "Overdue" => "#b91c1c",

            "Cancelled" => "#6b7280",

            _ => "#475569"
        };
    }
}