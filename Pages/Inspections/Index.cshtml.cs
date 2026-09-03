using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ServerRoomMonitor.Data;
using ServerRoomMonitor.Models;

namespace ServerRoomMonitor.Pages.Inspections;

[Authorize(Roles = "Admin,Technician")]
public class IndexModel : PageModel
{
private readonly ApplicationDbContext _context;


public IndexModel(ApplicationDbContext context)
{
    _context = context;
}

public ServerRoom? ServerRoom { get; set; } = new();

public List<Inspection> Inspections { get; set; } = new();

public async Task<IActionResult> OnGetAsync(int serverRoomId)
{
    ServerRoom = await _context.ServerRooms
        .FirstOrDefaultAsync(r => r.Id == serverRoomId);

    if (ServerRoom == null)
    {
        return NotFound();
    }

    Inspections = await _context.Inspections
        .Where(i => i.ServerRoomId == serverRoomId)
        .OrderByDescending(i => i.CheckedAt)
        .ToListAsync();

    return Page();
}


}