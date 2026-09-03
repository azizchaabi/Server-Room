using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ServerRoomMonitor.Pages;

[Authorize]
public class IndexModel : PageModel
{
    public IActionResult OnGet()
    {
        if (User.IsInRole("Admin"))
        {
            return RedirectToPage("/Admin/Index");
        }

        if (User.IsInRole("Technician"))
        {
            return RedirectToPage("/Technician/Index");
        }

        return Forbid();
    }
}
