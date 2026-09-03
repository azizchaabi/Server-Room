using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ServerRoomMonitor.Data;
using ServerRoomMonitor.Models;

namespace ServerRoomMonitor.Pages.Technician;

[Authorize(Roles = "Technician")]
public class IndexModel : PageModel
{
private readonly ApplicationDbContext _context;
private readonly UserManager<IdentityUser> _userManager;


public IndexModel(
    ApplicationDbContext context,
    UserManager<IdentityUser> userManager)
{
    _context = context;
    _userManager = userManager;
}

// =========================================================
// DATA
// =========================================================

public List<ScheduledInspection> ScheduledInspections { get; set; } = new();

public List<Reminder> Reminders { get; set; } = new();

// =========================================================
// ACTIVE SCHEDULED REMINDERS
// =========================================================

public List<ScheduledInspection> ActiveScheduledReminders =>
    ScheduledInspections
        .Where(s =>
            s.Status != "Completed" &&
            s.Status != "Cancelled")
        .OrderBy(s => s.Deadline)
        .ToList();

public int TotalReminderCount =>
    ActiveScheduledReminders.Count + Reminders.Count;

// =========================================================
// GET
// =========================================================

public async Task<IActionResult> OnGetAsync()
{
    var technician =
        await _userManager.GetUserAsync(User);

    if (technician == null)
    {
        return Challenge();
    }

    // -----------------------------------------------------
    // Load ONLY inspections assigned to this technician
    // -----------------------------------------------------

    ScheduledInspections =
        await _context.ScheduledInspections
            .Include(s => s.ServerRoom)
            .Include(s => s.Inspections)
                .ThenInclude(i => i.Technician)
            .Where(s => s.TechnicianId == technician.Id)
            .OrderBy(s => s.Status == "Completed")
            .ThenBy(s => s.ScheduledAt)
            .ToListAsync();

    // -----------------------------------------------------
    // Automatically mark expired inspections as overdue
    // -----------------------------------------------------

    var now = DateTime.Now;

    var changed = false;

    foreach (var inspection in ScheduledInspections)
    {
        if (inspection.Status != "Completed" &&
            inspection.Status != "Cancelled" &&
            inspection.Status != "Requires Fix" &&
            inspection.Status != "Awaiting Verification" &&
            inspection.Status != "Overdue" &&
            now > inspection.Deadline)
        {
            inspection.Status = "Overdue";
            changed = true;
        }
    }

    if (changed)
    {
        await _context.SaveChangesAsync();
    }

    // -----------------------------------------------------
    // Load reminders for server rooms assigned to this
    // technician.
    //
    // These are the existing 7-day/system reminders.
    // -----------------------------------------------------

    var assignedRoomIds = ScheduledInspections
        .Select(s => s.ServerRoomId)
        .Distinct()
        .ToList();

    Reminders = await _context.Reminders
        .Include(r => r.ServerRoom)
        .Where(r =>
            assignedRoomIds.Contains(r.ServerRoomId) &&
            !r.IsRead)
        .OrderByDescending(r => r.CreatedAt)
        .ToListAsync();

    return Page();
}

// =========================================================
// CHECK WHETHER AN INSPECTION CAN BE PERFORMED
// =========================================================

public bool CanInspect(
    ScheduledInspection inspection)
{
    var now = DateTime.Now;

    return inspection.AttemptCount < 3
        && inspection.ScheduledAt <= now
        && inspection.Deadline >= now
        &&
        (
            inspection.Status == "Scheduled" ||
            inspection.Status == "In Progress" ||
            inspection.Status == "Awaiting Verification"
        );
}

// =========================================================
// CHECK WHETHER A SCHEDULED INSPECTION NEEDS ATTENTION
// =========================================================

public bool NeedsAttention(
    ScheduledInspection inspection)
{
    return inspection.Status != "Completed" &&
           inspection.Status != "Cancelled";
}

// =========================================================
// GET REMINDER STATUS TEXT
// =========================================================

public string GetScheduledReminderMessage(
    ScheduledInspection inspection)
{
    var now = DateTime.Now;

    if (inspection.Status == "Overdue")
    {
        return "The deadline has passed. This inspection requires attention.";
    }

    if (inspection.Status == "Requires Fix")
    {
        return "The inspection failed three times and is waiting for administrator action.";
    }

    if (inspection.Status == "Awaiting Verification")
    {
        return "The server room has been fixed and is waiting for your verification.";
    }

    if (inspection.Status == "In Progress")
    {
        return $"Inspection attempt {inspection.AttemptCount}/3 has failed. Another attempt is available.";
    }

    if (inspection.ScheduledAt > now)
    {
        return "This inspection is scheduled and waiting to be performed.";
    }

    return "This inspection is ready to be performed.";
}

// =========================================================
// GET LAST RESULT
// =========================================================

public Inspection? GetLastInspection(
    ScheduledInspection scheduledInspection)
{
    return scheduledInspection.Inspections
        .OrderByDescending(i => i.CheckedAt)
        .FirstOrDefault();
}


}
