
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ServerRoomMonitor.Data;
using ServerRoomMonitor.Models;
using ServerRoomMonitor.Services;

namespace ServerRoomMonitor.Pages.Reports;

[Authorize(Roles = "Admin,Technician")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly ReportPdfService _reportPdfService;

    public IndexModel(
        ApplicationDbContext context,
        ReportPdfService reportPdfService)
    {
        _context = context;
        _reportPdfService = reportPdfService;
    }

    // =========================================================
    // PAGINATION
    // =========================================================

    [BindProperty(SupportsGet = true)]
    public int RoomPage { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int InspectionPage { get; set; } = 1;

    public int RoomPageSize { get; set; } = 8;

    public int InspectionPageSize { get; set; } = 8;

    public int TotalRoomPages { get; set; }

    public int TotalInspectionPages { get; set; }


    // =========================================================
    // SUMMARY
    // =========================================================

    public int TotalRooms { get; set; }

    public int TotalInspections { get; set; }

    public int SuccessfulInspections { get; set; }

    public int FailedInspections { get; set; }

    public int OverdueRooms { get; set; }

    public double ComplianceRate { get; set; }


    // =========================================================
    // REPORT DATA
    // =========================================================

    public List<RoomReport> RoomReports { get; set; } = new();

    public List<RecentInspection> RecentInspections { get; set; } = new();


    // =========================================================
    // GET
    // =========================================================

    public async Task OnGetAsync()
    {
        await LoadReportDataAsync();
    }


    // =========================================================
    // GENERATE PDF
    // =========================================================

    public async Task<IActionResult> OnGetGeneratePdfAsync()
    {
        // PDF should contain ALL data, not just the current page.
        await LoadAllReportDataAsync();

        var roomReportData = RoomReports
            .Select(room => new RoomReportData
            {
                Id = room.Id,
                Name = room.Name,
                Location = room.Location,
                LastInspection = room.LastInspection,
                DaysSinceInspection = room.DaysSinceInspection,
                TotalInspections = room.TotalInspections,
                FailedInspections = room.FailedInspections
            })
            .ToList();

        var recentInspectionData = RecentInspections
            .Select(inspection => new RecentInspectionData
            {
                Id = inspection.Id,
                CheckedAt = inspection.CheckedAt,
                RoomName = inspection.RoomName,
                TechnicianEmail = inspection.TechnicianEmail,
                Temperature = inspection.Temperature,
                IsOk = inspection.IsOk
            })
            .ToList();

        var pdf = _reportPdfService.GenerateReport(
            TotalRooms,
            TotalInspections,
            SuccessfulInspections,
            FailedInspections,
            OverdueRooms,
            ComplianceRate,
            roomReportData,
            recentInspectionData);

        return File(
            pdf,
            "application/pdf",
            $"ServerRoomReport_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
    }


    // =========================================================
    // LOAD DATA FOR PAGE
    // =========================================================

    private async Task LoadReportDataAsync()
    {
        if (RoomPage < 1)
            RoomPage = 1;

        if (InspectionPage < 1)
            InspectionPage = 1;

        var rooms = await _context.ServerRooms
            .Include(r => r.Inspections)
            .ToListAsync();

        // =====================================================
        // SUMMARY
        // =====================================================

        TotalRooms = rooms.Count;

        var allInspections = rooms
            .SelectMany(r => r.Inspections)
            .ToList();

        TotalInspections = allInspections.Count;

        SuccessfulInspections = allInspections.Count(i => i.IsOk);

        FailedInspections = allInspections.Count(i => !i.IsOk);

        if (TotalInspections > 0)
        {
            ComplianceRate = Math.Round(
                (double)SuccessfulInspections /
                TotalInspections *
                100,
                1);
        }
        else
        {
            ComplianceRate = 0;
        }


        // =====================================================
        // ROOM REPORTS
        // =====================================================

        var allRoomReports = new List<RoomReport>();

        OverdueRooms = 0;

        var now = DateTime.Now;

        foreach (var room in rooms)
        {
            var lastInspection = room.Inspections
                .OrderByDescending(i => i.CheckedAt)
                .FirstOrDefault();

            double daysSinceInspection = lastInspection == null
                ? double.MaxValue
                : (now - lastInspection.CheckedAt).TotalDays;

            if (lastInspection == null || daysSinceInspection >= 7)
            {
                OverdueRooms++;
            }

            allRoomReports.Add(new RoomReport
            {
                Id = room.Id,
                Name = room.Name,
                Location = room.Location,
                LastInspection = lastInspection?.CheckedAt,
                DaysSinceInspection = daysSinceInspection,
                TotalInspections = room.Inspections.Count,
                FailedInspections = room.Inspections.Count(i => !i.IsOk)
            });
        }

        // Calculate room pages
        TotalRoomPages = (int)Math.Ceiling(
            (double)allRoomReports.Count / RoomPageSize);

        if (TotalRoomPages == 0)
            TotalRoomPages = 1;

        if (RoomPage > TotalRoomPages)
            RoomPage = TotalRoomPages;

        // Take current room page
        RoomReports = allRoomReports
            .OrderBy(r => r.Name)
            .Skip((RoomPage - 1) * RoomPageSize)
            .Take(RoomPageSize)
            .ToList();


        // =====================================================
        // RECENT INSPECTIONS
        // =====================================================

        var allRecentInspections = allInspections
            .OrderByDescending(i => i.CheckedAt)
            .Select(i => new RecentInspection
            {
                Id = i.Id,
                CheckedAt = i.CheckedAt,

                RoomName = rooms
                    .First(r => r.Id == i.ServerRoomId)
                    .Name,

                TechnicianEmail = i.TechnicianId ?? "Not recorded",

                Temperature = i.Temperature,

                IsOk = i.IsOk
            })
            .ToList();

        // Calculate inspection pages
        TotalInspectionPages = (int)Math.Ceiling(
            (double)allRecentInspections.Count /
            InspectionPageSize);

        if (TotalInspectionPages == 0)
            TotalInspectionPages = 1;

        if (InspectionPage > TotalInspectionPages)
            InspectionPage = TotalInspectionPages;

        // Take current inspection page
        RecentInspections = allRecentInspections
            .Skip((InspectionPage - 1) * InspectionPageSize)
            .Take(InspectionPageSize)
            .ToList();
    }


    // =========================================================
    // LOAD ALL DATA FOR PDF
    // =========================================================

    private async Task LoadAllReportDataAsync()
    {
        var rooms = await _context.ServerRooms
            .Include(r => r.Inspections)
            .ToListAsync();

        TotalRooms = rooms.Count;

        var allInspections = rooms
            .SelectMany(r => r.Inspections)
            .ToList();

        TotalInspections = allInspections.Count;

        SuccessfulInspections = allInspections.Count(i => i.IsOk);

        FailedInspections = allInspections.Count(i => !i.IsOk);

        if (TotalInspections > 0)
        {
            ComplianceRate = Math.Round(
                (double)SuccessfulInspections /
                TotalInspections *
                100,
                1);
        }
        else
        {
            ComplianceRate = 0;
        }

        OverdueRooms = 0;

        RoomReports = new();

        var now = DateTime.Now;

        foreach (var room in rooms)
        {
            var lastInspection = room.Inspections
                .OrderByDescending(i => i.CheckedAt)
                .FirstOrDefault();

            double daysSinceInspection = lastInspection == null
                ? double.MaxValue
                : (now - lastInspection.CheckedAt).TotalDays;

            if (lastInspection == null || daysSinceInspection >= 7)
            {
                OverdueRooms++;
            }

            RoomReports.Add(new RoomReport
            {
                Id = room.Id,
                Name = room.Name,
                Location = room.Location,
                LastInspection = lastInspection?.CheckedAt,
                DaysSinceInspection = daysSinceInspection,
                TotalInspections = room.Inspections.Count,
                FailedInspections = room.Inspections.Count(i => !i.IsOk)
            });
        }

        RecentInspections = allInspections
            .OrderByDescending(i => i.CheckedAt)
            .Select(i => new RecentInspection
            {
                Id = i.Id,

                CheckedAt = i.CheckedAt,

                RoomName = rooms
                    .First(r => r.Id == i.ServerRoomId)
                    .Name,

                TechnicianEmail = i.TechnicianId ?? "Not recorded",

                Temperature = i.Temperature,

                IsOk = i.IsOk
            })
            .ToList();
    }


    // =========================================================
    // ROOM REPORT MODEL
    // =========================================================

    public class RoomReport
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";

        public string Location { get; set; } = "";

        public DateTime? LastInspection { get; set; }

        public double DaysSinceInspection { get; set; }

        public int TotalInspections { get; set; }

        public int FailedInspections { get; set; }
    }


    // =========================================================
    // RECENT INSPECTION MODEL
    // =========================================================

    public class RecentInspection
    {
        public int Id { get; set; }

        public DateTime CheckedAt { get; set; }

        public string RoomName { get; set; } = "";

        public string TechnicianEmail { get; set; } = "";

        public decimal Temperature { get; set; }

        public bool IsOk { get; set; }
    }
}

