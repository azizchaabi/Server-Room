using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ServerRoomMonitor.Data;
using ServerRoomMonitor.Models;

namespace ServerRoomMonitor.Pages.ServerRooms;

public class IndexModel : PageModel
{
private readonly ApplicationDbContext _context;


public IndexModel(ApplicationDbContext context)
{
    _context = context;
}

// =========================================================
// SERVER ROOMS
// =========================================================

public List<ServerRoom> ServerRooms { get; set; } = new();

public int TotalRooms { get; set; }

// =========================================================
// PAGINATION
// =========================================================

[BindProperty(SupportsGet = true)]
public int PageNumber { get; set; } = 1;

public int PageSize { get; set; } = 6;

public int TotalPages { get; set; }

// =========================================================
// GET
// =========================================================

public async Task<IActionResult> OnGetAsync()
{
    // -----------------------------------------------------
    // Make sure page number is valid
    // -----------------------------------------------------

    if (PageNumber < 1)
    {
        PageNumber = 1;
    }

    // -----------------------------------------------------
    // Count all server rooms
    // -----------------------------------------------------

    TotalRooms = await _context.ServerRooms.CountAsync();

    // -----------------------------------------------------
    // Calculate total number of pages
    // -----------------------------------------------------

    TotalPages = (int)Math.Ceiling(
        TotalRooms / (double)PageSize);

    // -----------------------------------------------------
    // If someone enters a page number that does not exist,
    // send them to the last valid page.
    // -----------------------------------------------------

    if (TotalPages > 0 && PageNumber > TotalPages)
    {
        PageNumber = TotalPages;
    }

    // -----------------------------------------------------
    // Load ONLY the rooms for the current page
    // -----------------------------------------------------

    ServerRooms = await _context.ServerRooms
        .OrderBy(r => r.Name)
        .Skip((PageNumber - 1) * PageSize)
        .Take(PageSize)
        .ToListAsync();

    return Page();
}


}
