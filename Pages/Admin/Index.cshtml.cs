using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ServerRoomMonitor.Data;
using ServerRoomMonitor.Models;

namespace ServerRoomMonitor.Pages.Admin;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }


    // =========================================================
    // DATA
    // =========================================================

    public List<ServerRoom> ServerRooms { get; set; } = new();

    public List<Reminder> Reminders { get; set; } = new();

    public List<Inspection> RecentInspections { get; set; } = new();


    // =========================================================
    // DASHBOARD STATISTICS
    // =========================================================

    public int TotalRooms { get; set; }

    public int UpToDateRooms { get; set; }

    public int OverdueRooms { get; set; }

    public int ActiveReminders { get; set; }


    // =========================================================
    // PAGINATION
    // =========================================================

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public const int PageSize = 5;

    public int TotalInspections { get; set; }

    public int TotalPages =>
        (int)Math.Ceiling(
            TotalInspections / (double)PageSize
        );

    public int CurrentPage =>
        Math.Max(
            1,
            Math.Min(PageNumber, Math.Max(TotalPages, 1))
        );

    public int StartInspection =>
        TotalInspections == 0
            ? 0
            : ((CurrentPage - 1) * PageSize) + 1;

    public int EndInspection =>
        Math.Min(
            CurrentPage * PageSize,
            TotalInspections
        );


    // =========================================================
    // LOAD DASHBOARD
    // =========================================================

    public async Task OnGetAsync()
    {

        // -----------------------------------------------------
        // SERVER ROOMS
        // -----------------------------------------------------

        ServerRooms = await _context.ServerRooms
            .Include(r => r.Inspections)
            .ThenInclude(i => i.Technician)
            .ToListAsync();


        // -----------------------------------------------------
        // ACTIVE REMINDERS
        // -----------------------------------------------------

        Reminders = await _context.Reminders
            .Include(r => r.ServerRoom)
            .Where(r => !r.IsRead)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();


        // -----------------------------------------------------
        // TOTAL INSPECTIONS
        // -----------------------------------------------------

        TotalInspections = await _context.Inspections
            .CountAsync();


        // -----------------------------------------------------
        // RECENT INSPECTIONS
        // -----------------------------------------------------

        RecentInspections = await _context.Inspections
            .Include(i => i.ServerRoom)
            .Include(i => i.Technician)
            .OrderByDescending(i => i.CheckedAt)
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();


        // -----------------------------------------------------
        // ROOM STATUS
        // -----------------------------------------------------

        TotalRooms = ServerRooms.Count;

        foreach (var room in ServerRooms)
        {
            var lastInspection = room.Inspections
                .OrderByDescending(i => i.CheckedAt)
                .FirstOrDefault();


            // No inspection has ever been performed
            if (lastInspection == null)
            {
                OverdueRooms++;
                continue;
            }


            var daysSinceInspection =
                (DateTime.Now - lastInspection.CheckedAt).TotalDays;


            // Inspection older than 7 days
            if (daysSinceInspection >= 7)
            {
                OverdueRooms++;
            }
            else
            {
                UpToDateRooms++;
            }
        }


        // -----------------------------------------------------
        // ACTIVE REMINDERS
        // -----------------------------------------------------

        ActiveReminders = Reminders.Count;
    }
}

