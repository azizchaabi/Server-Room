using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ServerRoomMonitor.Data;
using ServerRoomMonitor.Models;

namespace ServerRoomMonitor.Pages.ServerRooms;

public class DeleteModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DeleteModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public ServerRoom ServerRoom { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var serverRoom = await _context.ServerRooms
            .FirstOrDefaultAsync(r => r.Id == id);

        if (serverRoom == null)
        {
            return NotFound();
        }

        ServerRoom = serverRoom;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var serverRoom = await _context.ServerRooms
            .FindAsync(id);

        if (serverRoom == null)
        {
            return NotFound();
        }

        _context.ServerRooms.Remove(serverRoom);

        await _context.SaveChangesAsync();

        return RedirectToPage("Index");
    }
}
