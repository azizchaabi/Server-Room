using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ServerRoomMonitor.Data;
using ServerRoomMonitor.Models;

namespace ServerRoomMonitor.Pages.ServerRooms;

public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DetailsModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public ServerRoom ServerRoom { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var serverRoom = await _context.ServerRooms
            .Include(r => r.Inspections)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (serverRoom == null)
        {
            return NotFound();
        }

        ServerRoom = serverRoom;

        return Page();
    }
}
