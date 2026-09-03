using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ServerRoomMonitor.Data;
using ServerRoomMonitor.Models;

namespace ServerRoomMonitor.Pages.ServerRooms;

public class EditModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public EditModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public ServerRoom ServerRoom { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var serverRoom = await _context.ServerRooms.FindAsync(id);

        if (serverRoom == null)
        {
            return NotFound();
        }

        ServerRoom = serverRoom;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var serverRoom = await _context.ServerRooms.FindAsync(ServerRoom.Id);

        if (serverRoom == null)
        {
            return NotFound();
        }

        serverRoom.Name = ServerRoom.Name;
        serverRoom.Location = ServerRoom.Location;

        await _context.SaveChangesAsync();

        return RedirectToPage("Index");
    }
}

