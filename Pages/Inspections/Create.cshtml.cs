using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ServerRoomMonitor.Data;
using ServerRoomMonitor.Models;
using ServerRoomMonitor.Services;

namespace ServerRoomMonitor.Pages.Inspections;

[Authorize(Roles = "Technician")]
public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly EmailNotificationService _emailService;

    public CreateModel(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager,
        EmailNotificationService emailService)
    {
        _context = context;
        _userManager = userManager;
        _emailService = emailService;
    }

    [BindProperty]
    public Inspection Inspection { get; set; } = new();

    public ServerRoom ServerRoom { get; set; } = new();

    public ScheduledInspection ScheduledInspection { get; set; } = new();

    // =============================================================
    // GET
    // =============================================================

    public async Task<IActionResult> OnGetAsync(
        int scheduledInspectionId)
    {
        var technician =
            await _userManager.GetUserAsync(User);

        if (technician == null)
        {
            return Challenge();
        }

        ScheduledInspection =
            await _context.ScheduledInspections
                .Include(s => s.ServerRoom)
                .Include(s => s.Inspections)
                .FirstOrDefaultAsync(s =>
                    s.Id == scheduledInspectionId &&
                    s.TechnicianId == technician.Id);

        if (ScheduledInspection == null)
        {
            return NotFound();
        }

        ServerRoom =
            ScheduledInspection.ServerRoom!;

        // ---------------------------------------------------------
        // Inspection cannot start before scheduled time
        // ---------------------------------------------------------

        if (DateTime.Now < ScheduledInspection.ScheduledAt)
        {
            TempData["ErrorMessage"] =
                "This inspection is not available yet.";

            return RedirectToPage(
                "/Technician/Index");
        }

        // ---------------------------------------------------------
        // Cancelled
        // ---------------------------------------------------------

        if (ScheduledInspection.Status == "Cancelled")
        {
            TempData["ErrorMessage"] =
                "This inspection has been cancelled.";

            return RedirectToPage(
                "/Technician/Index");
        }

        // ---------------------------------------------------------
        // Completed
        // ---------------------------------------------------------

        if (ScheduledInspection.Status == "Completed")
        {
            TempData["ErrorMessage"] =
                "This inspection has already been completed.";

            return RedirectToPage(
                "/Technician/Index");
        }

        // ---------------------------------------------------------
        // Requires fix
        // ---------------------------------------------------------

        if (ScheduledInspection.Status == "Requires Fix")
        {
            TempData["ErrorMessage"] =
                "This inspection requires an administrator to fix the server room first.";

            return RedirectToPage(
                "/Technician/Index");
        }

        // ---------------------------------------------------------
        // Deadline
        // ---------------------------------------------------------

        if (DateTime.Now > ScheduledInspection.Deadline)
        {
            ScheduledInspection.Status =
                "Overdue";

            await _context.SaveChangesAsync();

            TempData["ErrorMessage"] =
                "The deadline for this inspection has passed.";

            return RedirectToPage(
                "/Technician/Index");
        }

        // ---------------------------------------------------------
        // Maximum attempts
        // ---------------------------------------------------------

        if (ScheduledInspection.AttemptCount >= 3)
        {
            TempData["ErrorMessage"] =
                "The maximum number of inspection attempts has been reached.";

            return RedirectToPage(
                "/Technician/Index");
        }

        // ---------------------------------------------------------
        // Prepare inspection
        // ---------------------------------------------------------

        Inspection.ServerRoomId =
            ScheduledInspection.ServerRoomId;

        Inspection.ScheduledInspectionId =
            ScheduledInspection.Id;

        Inspection.TechnicianId =
            technician.Id;

        Inspection.CheckedAt =
            DateTime.Now;

        Inspection.AttemptNumber =
            ScheduledInspection.AttemptCount + 1;

        return Page();
    }

    // =============================================================
    // POST
    // =============================================================

    public async Task<IActionResult> OnPostAsync()
    {
        var technician =
            await _userManager.GetUserAsync(User);

        if (technician == null)
        {
            return Challenge();
        }

        ScheduledInspection =
            await _context.ScheduledInspections
                .Include(s => s.ServerRoom)
                .Include(s => s.Inspections)
                .FirstOrDefaultAsync(s =>
                    s.Id == Inspection.ScheduledInspectionId &&
                    s.TechnicianId == technician.Id);

        if (ScheduledInspection == null)
        {
            return NotFound();
        }

        ServerRoom =
            ScheduledInspection.ServerRoom!;

        // ---------------------------------------------------------
        // Not available yet
        // ---------------------------------------------------------

        if (DateTime.Now < ScheduledInspection.ScheduledAt)
        {
            ModelState.AddModelError(
                "",
                "This inspection is not available yet.");

            return Page();
        }

        // ---------------------------------------------------------
        // Deadline
        // ---------------------------------------------------------

        if (DateTime.Now > ScheduledInspection.Deadline)
        {
            ScheduledInspection.Status =
                "Overdue";

            await _context.SaveChangesAsync();

            ModelState.AddModelError(
                "",
                "The deadline for this inspection has passed.");

            return Page();
        }

        // ---------------------------------------------------------
        // Cancelled
        // ---------------------------------------------------------

        if (ScheduledInspection.Status == "Cancelled")
        {
            ModelState.AddModelError(
                "",
                "This inspection has been cancelled.");

            return Page();
        }

        // ---------------------------------------------------------
        // Completed
        // ---------------------------------------------------------

        if (ScheduledInspection.Status == "Completed")
        {
            ModelState.AddModelError(
                "",
                "This inspection has already been completed.");

            return Page();
        }

        // ---------------------------------------------------------
        // Requires fix
        // ---------------------------------------------------------

        if (ScheduledInspection.Status == "Requires Fix")
        {
            ModelState.AddModelError(
                "",
                "This inspection requires an administrator to fix the server room first.");

            return Page();
        }

        // ---------------------------------------------------------
        // Maximum attempts
        // ---------------------------------------------------------

        if (ScheduledInspection.AttemptCount >= 3)
        {
            ModelState.AddModelError(
                "",
                "The maximum number of inspection attempts has been reached.");

            return Page();
        }

        // ---------------------------------------------------------
        // Validate form
        // ---------------------------------------------------------

        if (!ModelState.IsValid)
        {
            Inspection.AttemptNumber =
                ScheduledInspection.AttemptCount + 1;

            Inspection.ServerRoomId =
                ScheduledInspection.ServerRoomId;

            Inspection.ScheduledInspectionId =
                ScheduledInspection.Id;

            return Page();
        }

        // ---------------------------------------------------------
        // Set inspection information
        // ---------------------------------------------------------

        Inspection.AttemptNumber =
            ScheduledInspection.AttemptCount + 1;

        Inspection.ServerRoomId =
            ScheduledInspection.ServerRoomId;

        Inspection.TechnicianId =
            technician.Id;

        Inspection.CheckedAt =
            DateTime.Now;

        // ---------------------------------------------------------
        // Calculate temperature status
        // ---------------------------------------------------------

        Inspection.TemperatureOk =
            Inspection.Temperature >= 18 &&
            Inspection.Temperature <= 27;

        // ---------------------------------------------------------
        // Calculate overall inspection result
        // ---------------------------------------------------------

        Inspection.IsOk =
            Inspection.TemperatureOk &&
            Inspection.AirConditioningOk &&
            Inspection.NoOverheatingAlarm &&
            Inspection.NoWaterLeak &&
            Inspection.PowerOk &&
            Inspection.RoomClean;

        // ---------------------------------------------------------
        // Save inspection
        // ---------------------------------------------------------

        _context.Inspections.Add(
            Inspection);

        // Increase attempt count.
        ScheduledInspection.AttemptCount++;

        // ---------------------------------------------------------
        // Determine scheduled inspection status
        // ---------------------------------------------------------

        if (Inspection.IsOk)
        {
            // Successful inspection.
            ScheduledInspection.Status =
                "Completed";

            ServerRoom.Status =
                "Operational";
        }
        else if (ScheduledInspection.AttemptCount >= 3)
        {
            // Third failed attempt.
            ScheduledInspection.Status =
                "Requires Fix";

            ServerRoom.Status =
                "Requires Fix";
        }
        else
        {
            // Attempt 1 or 2 failed.
            ScheduledInspection.Status =
                "In Progress";
        }

        // ---------------------------------------------------------
        // Resolve reminders ONLY after successful inspection
        // ---------------------------------------------------------

        if (Inspection.IsOk)
        {
            var unreadReminders =
                await _context.Reminders
                    .Where(r =>
                        r.ServerRoomId ==
                            Inspection.ServerRoomId &&
                        !r.IsRead)
                    .ToListAsync();

            foreach (var reminder in unreadReminders)
            {
                reminder.IsRead = true;
            }
        }

        // ---------------------------------------------------------
        // Save everything
        // ---------------------------------------------------------

        await _context.SaveChangesAsync();

        // =========================================================
        // FAILURE EMAIL
        // =========================================================
        //
        // IMPORTANT:
        //
        // Attempt 1 -> NO EMAIL
        // Attempt 2 -> NO EMAIL
        // Attempt 3 -> EMAIL ADMINS
        //
        // =========================================================

        if (!Inspection.IsOk &&
            ScheduledInspection.AttemptCount >= 3)
        {
            var failedChecks =
                new List<string>();

            if (!Inspection.TemperatureOk)
            {
                failedChecks.Add(
                    $"Temperature: {Inspection.Temperature}°C (expected 18-27°C)");
            }

            if (!Inspection.AirConditioningOk)
            {
                failedChecks.Add(
                    "Air conditioning: NOT OK");
            }

            if (!Inspection.NoOverheatingAlarm)
            {
                failedChecks.Add(
                    "Overheating alarm: PROBLEM DETECTED");
            }

            if (!Inspection.NoWaterLeak)
            {
                failedChecks.Add(
                    "Water leak: PROBLEM DETECTED");
            }

            if (!Inspection.PowerOk)
            {
                failedChecks.Add(
                    "Power: NOT OK");
            }

            if (!Inspection.RoomClean)
            {
                failedChecks.Add(
                    "Room cleanliness: NOT OK");
            }

            var failedChecksText =
                failedChecks.Any()
                    ? string.Join(
                        "\n",
                        failedChecks)
                    : "One or more inspection checks failed.";

            var message =
                $"""
                Inspection attempt:
                {Inspection.AttemptNumber}/3

                Inspection performed by:
                {technician.Email}

                Inspection time:
                {Inspection.CheckedAt:dd/MM/yyyy HH:mm}

                Failed checks:
                {failedChecksText}

                Notes:
                {Inspection.Notes ?? "None"}

                Current status:
                {ScheduledInspection.Status}

                Remaining attempts:
                0

                The server room requires administrator attention.
                """;

            await _emailService.SendInspectionFailureAsync(
                ServerRoom.Name,
                ServerRoom.Id,
                message);
        }

        // =========================================================
        // RESULT MESSAGE
        // =========================================================

        if (Inspection.IsOk)
        {
            TempData["SuccessMessage"] =
                "Inspection completed successfully.";
        }
        else if (
            ScheduledInspection.Status ==
            "Requires Fix")
        {
            TempData["ErrorMessage"] =
                "The third inspection failed. The server room requires fixing by an administrator.";
        }
        else
        {
            TempData["ErrorMessage"] =
                $"Inspection attempt {Inspection.AttemptNumber} failed. " +
                "Another attempt is allowed.";
        }

        return RedirectToPage(
            "/Technician/Index");
    }
}