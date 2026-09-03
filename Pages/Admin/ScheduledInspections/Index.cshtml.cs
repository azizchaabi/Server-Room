using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ServerRoomMonitor.Data;
using ServerRoomMonitor.Models;
using ServerRoomMonitor.Services;

namespace ServerRoomMonitor.Pages.Admin.Inspections;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly EmailNotificationService _emailService;

    public IndexModel(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager,
        EmailNotificationService emailService)
    {
        _context = context;
        _userManager = userManager;
        _emailService = emailService;
    }

    public List<ServerRoom> ServerRooms { get; set; } = new();

    public List<IdentityUser> Technicians { get; set; } = new();

    public List<ScheduledInspection> ScheduledInspections { get; set; } = new();

    [BindProperty]
    public int ServerRoomId { get; set; }

    [BindProperty]
    public string? TechnicianId { get; set; }

    // Used by the browser: yyyy-MM-ddTHH:mm
    [BindProperty]
    public string? ScheduledAt { get; set; }

    // Used by the browser: yyyy-MM-ddTHH:mm
    [BindProperty]
    public string? Deadline { get; set; }

    [BindProperty]
    public string? Notes { get; set; }

    public async Task OnGetAsync()
    {
        var now = DateTime.Now;

        // Default inspection time:
        // current time with seconds removed.
        ScheduledAt = now.ToString("yyyy-MM-ddTHH:mm");

        // Default deadline:
        // 24 hours after scheduled time.
        Deadline = now
            .AddDays(1)
            .ToString("yyyy-MM-ddTHH:mm");

        await LoadDataAsync();
    }

    public async Task<IActionResult> OnPostScheduleAsync()
    {
        await LoadDataAsync();

        // ---------------------------------------------------------
        // Validate server room
        // ---------------------------------------------------------

        if (ServerRoomId <= 0)
        {
            ModelState.AddModelError(
                nameof(ServerRoomId),
                "Please select a server room.");
        }

        // ---------------------------------------------------------
        // Validate technician
        // ---------------------------------------------------------

        if (string.IsNullOrWhiteSpace(TechnicianId))
        {
            ModelState.AddModelError(
                nameof(TechnicianId),
                "Please select a technician.");
        }

        // ---------------------------------------------------------
        // Validate scheduled date/time
        // ---------------------------------------------------------

        DateTime scheduledDateTime = default;

        if (string.IsNullOrWhiteSpace(ScheduledAt))
        {
            ModelState.AddModelError(
                nameof(ScheduledAt),
                "Please select a date and time.");
        }
        else if (!DateTime.TryParse(
                     ScheduledAt,
                     out scheduledDateTime))
        {
            ModelState.AddModelError(
                nameof(ScheduledAt),
                "Please enter a valid date and time.");
        }
        else if (scheduledDateTime < DateTime.Now)
        {
            ModelState.AddModelError(
                nameof(ScheduledAt),
                "The inspection cannot be scheduled in the past.");
        }

        // ---------------------------------------------------------
        // Validate deadline
        // ---------------------------------------------------------

        DateTime deadlineDateTime = default;

        if (string.IsNullOrWhiteSpace(Deadline))
        {
            ModelState.AddModelError(
                nameof(Deadline),
                "Please select a deadline.");
        }
        else if (!DateTime.TryParse(
                     Deadline,
                     out deadlineDateTime))
        {
            ModelState.AddModelError(
                nameof(Deadline),
                "Please enter a valid deadline.");
        }

        // ---------------------------------------------------------
        // Validate server room exists
        // ---------------------------------------------------------

        var serverRoom = await _context.ServerRooms
            .FirstOrDefaultAsync(r => r.Id == ServerRoomId);

        if (serverRoom == null)
        {
            ModelState.AddModelError(
                nameof(ServerRoomId),
                "The selected server room does not exist.");
        }

        // ---------------------------------------------------------
        // Validate technician
        // ---------------------------------------------------------

        IdentityUser? technician = null;

        if (!string.IsNullOrWhiteSpace(TechnicianId))
        {
            technician = await _userManager
                .FindByIdAsync(TechnicianId);

            if (technician == null)
            {
                ModelState.AddModelError(
                    nameof(TechnicianId),
                    "The selected technician does not exist.");
            }
            else if (!await _userManager.IsInRoleAsync(
                         technician,
                         "Technician"))
            {
                ModelState.AddModelError(
                    nameof(TechnicianId),
                    "The selected user is not a technician.");
            }
            else if (string.IsNullOrWhiteSpace(technician.Email))
            {
                ModelState.AddModelError(
                    nameof(TechnicianId),
                    "The selected technician does not have an email address.");
            }
        }

        // ---------------------------------------------------------
        // Deadline must be after scheduled time
        // ---------------------------------------------------------

        if (DateTime.TryParse(
                ScheduledAt,
                out scheduledDateTime) &&
            DateTime.TryParse(
                Deadline,
                out deadlineDateTime))
        {
            if (deadlineDateTime <= scheduledDateTime)
            {
                ModelState.AddModelError(
                    nameof(Deadline),
                    "The deadline must be after the scheduled time.");
            }
        }

        // ---------------------------------------------------------
        // Stop if validation failed
        // ---------------------------------------------------------

        if (!ModelState.IsValid)
        {
            return Page();
        }

        // ---------------------------------------------------------
        // Remove seconds and milliseconds
        // ---------------------------------------------------------

        scheduledDateTime = new DateTime(
            scheduledDateTime.Year,
            scheduledDateTime.Month,
            scheduledDateTime.Day,
            scheduledDateTime.Hour,
            scheduledDateTime.Minute,
            0);

        deadlineDateTime = new DateTime(
            deadlineDateTime.Year,
            deadlineDateTime.Month,
            deadlineDateTime.Day,
            deadlineDateTime.Hour,
            deadlineDateTime.Minute,
            0);

        // ---------------------------------------------------------
        // Create scheduled inspection
        // ---------------------------------------------------------

        var scheduledInspection = new ScheduledInspection
        {
            ServerRoomId = ServerRoomId,
            TechnicianId = TechnicianId!,
            ScheduledAt = scheduledDateTime,
            Deadline = deadlineDateTime,
            Status = "Scheduled",
            AttemptCount = 0,
            Notes = Notes ?? "",
            CreatedAt = DateTime.Now
        };

        _context.ScheduledInspections.Add(
            scheduledInspection);

        await _context.SaveChangesAsync();

        // ---------------------------------------------------------
        // Email assigned technician
        // ---------------------------------------------------------

        if (technician != null &&
            !string.IsNullOrWhiteSpace(technician.Email))
        {
            await _emailService.SendScheduledInspectionAsync(
                technician.Email,
                serverRoom!.Name,
                scheduledInspection.ServerRoomId,
                scheduledInspection.ScheduledAt,
                scheduledInspection.Deadline,
                scheduledInspection.Notes);
        }

        TempData["SuccessMessage"] =
            "Inspection scheduled successfully.";

        return RedirectToPage();
    }

    // -------------------------------------------------------------
    // Cancel scheduled inspection
    // -------------------------------------------------------------

    public async Task<IActionResult> OnPostCancelAsync(int id)
    {
        var scheduledInspection =
            await _context.ScheduledInspections
                .FirstOrDefaultAsync(s => s.Id == id);

        if (scheduledInspection == null)
        {
            return NotFound();
        }

        if (scheduledInspection.Status == "Completed")
        {
            TempData["ErrorMessage"] =
                "A completed inspection cannot be cancelled.";

            return RedirectToPage();
        }

        if (scheduledInspection.Status == "Cancelled")
        {
            TempData["ErrorMessage"] =
                "This inspection is already cancelled.";

            return RedirectToPage();
        }

        scheduledInspection.Status = "Cancelled";

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "Scheduled inspection cancelled.";

        return RedirectToPage();
    }

    // -------------------------------------------------------------
    // Mark room fixed
    // -------------------------------------------------------------

    public async Task<IActionResult> OnPostMarkFixedAsync(int id)
    {
        var scheduledInspection =
            await _context.ScheduledInspections
                .FirstOrDefaultAsync(s => s.Id == id);

        if (scheduledInspection == null)
        {
            return NotFound();
        }

        if (scheduledInspection.Status != "Requires Fix")
        {
            TempData["ErrorMessage"] =
                "This inspection does not currently require a fix.";

            return RedirectToPage();
        }

        var admin = await _userManager.GetUserAsync(User);

        if (admin == null)
        {
            return Challenge();
        }

        // Administrator has confirmed that the physical problem
        // has been fixed. A technician must now verify the room.

        scheduledInspection.Status =
            "Awaiting Verification";

        scheduledInspection.FixedAt =
            DateTime.Now;

        scheduledInspection.FixedByAdminId =
            admin.Id;

        // Start a new verification cycle.
        scheduledInspection.AttemptCount = 0;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "The server room has been marked as fixed and is now awaiting technician verification.";

        return RedirectToPage();
    }

    // -------------------------------------------------------------
    // Load page data
    // -------------------------------------------------------------

    private async Task LoadDataAsync()
    {
        ServerRooms = await _context.ServerRooms
            .OrderBy(r => r.Name)
            .ToListAsync();

        var users = await _userManager
            .GetUsersInRoleAsync("Technician");

        Technicians = users
            .Where(u =>
                !string.IsNullOrWhiteSpace(u.Email))
            .OrderBy(u => u.UserName)
            .ToList();

        ScheduledInspections =
            await _context.ScheduledInspections
                .Include(s => s.ServerRoom)
                .Include(s => s.Technician)
                .Include(s => s.FixedByAdmin)
                .OrderBy(s => s.ScheduledAt)
                .ToListAsync();
    }
}