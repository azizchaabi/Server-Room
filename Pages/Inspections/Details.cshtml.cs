using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ServerRoomMonitor.Data;
using ServerRoomMonitor.Models;
using ServerRoomMonitor.Services;

namespace ServerRoomMonitor.Pages.Inspections;

[Authorize(Roles = "Admin,Technician")]
public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly InspectionPdfService _pdfService;

    public DetailsModel(
        ApplicationDbContext context,
        InspectionPdfService pdfService)
    {
        _context = context;
        _pdfService = pdfService;
    }

    public Inspection Inspection { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Inspection = await _context.Inspections
            .Include(i => i.ServerRoom)
            .Include(i => i.Technician)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (Inspection == null)
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnGetPdfAsync(int id)
    {
        var inspection = await _context.Inspections
            .Include(i => i.ServerRoom)
            .Include(i => i.Technician)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (inspection == null)
        {
            return NotFound();
        }

        var pdf = _pdfService.GenerateInspectionReport(
            inspection);

        var roomName =
            inspection.ServerRoom?.Name ?? "ServerRoom";

        var fileName =
            $"Inspection_{roomName}_{inspection.CheckedAt:yyyyMMdd_HHmm}.pdf";

        return File(
            pdf,
            "application/pdf",
            fileName);
    }
}