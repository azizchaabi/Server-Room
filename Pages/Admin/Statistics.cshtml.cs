using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ServerRoomMonitor.Data;

namespace ServerRoomMonitor.Pages.Admin;

[Authorize(Roles = "Admin")]
public class StatisticsModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public StatisticsModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public int TotalRooms { get; set; }

    public int TotalInspections { get; set; }

    public int SuccessfulInspections { get; set; }

    public int FailedInspections { get; set; }

    public int OverdueRooms { get; set; }

    public double ComplianceRate { get; set; }

    public List<string> InspectionDates { get; set; } = new();

    public List<int> InspectionCounts { get; set; } = new();

    public List<string> RoomNames { get; set; } = new();

    public List<int> RoomInspectionCounts { get; set; } = new();

    public List<int> RoomFailureCounts { get; set; } = new();

    public async Task OnGetAsync()
    {
        var rooms = await _context.ServerRooms
            .Include(r => r.Inspections)
            .ToListAsync();

        var inspections = await _context.Inspections
            .OrderBy(i => i.CheckedAt)
            .ToListAsync();

        TotalRooms = rooms.Count;

        TotalInspections = inspections.Count;

        SuccessfulInspections = inspections.Count(i => i.IsOk);

        FailedInspections = inspections.Count(i => !i.IsOk);

        if (TotalInspections > 0)
        {
            ComplianceRate = Math.Round(
                (double)SuccessfulInspections / TotalInspections * 100,
                1);
        }

        var now = DateTime.Now;

        foreach (var room in rooms)
        {
            var lastInspection = room.Inspections
                .OrderByDescending(i => i.CheckedAt)
                .FirstOrDefault();

            if (lastInspection == null)
            {
                OverdueRooms++;
                continue;
            }

            var daysSinceInspection =
                (now - lastInspection.CheckedAt).TotalDays;

            if (daysSinceInspection >= 7)
            {
                OverdueRooms++;
            }
        }

        var inspectionsByDate = inspections
            .GroupBy(i => i.CheckedAt.Date)
            .OrderBy(g => g.Key)
            .ToList();

        foreach (var group in inspectionsByDate)
        {
            InspectionDates.Add(
                group.Key.ToString("dd/MM"));

            InspectionCounts.Add(group.Count());
        }

        foreach (var room in rooms.OrderBy(r => r.Name))
        {
            RoomNames.Add(room.Name);

            RoomInspectionCounts.Add(
                room.Inspections.Count);

            RoomFailureCounts.Add(
                room.Inspections.Count(i => !i.IsOk));
        }
    }
}