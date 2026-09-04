using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ServerRoomMonitor.Data;
using ServerRoomMonitor.ML;
using ServerRoomMonitor.Models;

namespace ServerRoomMonitor.Pages.Admin.PredictiveMaintenance.Room;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
private readonly ApplicationDbContext _context;
private readonly PredictiveMaintenancePredictionService _predictionService;


public IndexModel(
    ApplicationDbContext context,
    PredictiveMaintenancePredictionService predictionService)
{
    _context = context;
    _predictionService = predictionService;
}

public RoomViewModel? Room { get; set; }

public List<HistoryViewModel> History { get; set; } = new();

public async Task OnGetAsync(int id)
{
    var serverRoom = await _context.ServerRooms
        .Include(r => r.Inspections)
        .Include(r => r.ScheduledInspections)
        .FirstOrDefaultAsync(r => r.Id == id);

    if (serverRoom == null)
    {
        return;
    }

    var inspections = serverRoom.Inspections
        .OrderByDescending(i => i.CheckedAt)
        .ToList();

    if (inspections.Count == 0)
    {
        Room = new RoomViewModel
        {
            ServerRoomId = serverRoom.Id,
            RoomName = serverRoom.Name,
            Location = serverRoom.Location,
            HasPrediction = false
        };

        return;
    }

    var latestInspection = inspections.First();

    var now = DateTime.Now;

    int daysSinceLastInspection =
        Math.Max(
            0,
            (int)(now - latestInspection.CheckedAt).TotalDays);

    int failedInspectionsLast7Days =
        inspections.Count(i =>
            i.CheckedAt >= now.AddDays(-7) &&
            !i.IsOk);

    int failedInspectionsLast30Days =
        inspections.Count(i =>
            i.CheckedAt >= now.AddDays(-30) &&
            !i.IsOk);

    int failedAttemptsLast30Days =
        inspections.Count(i =>
            i.CheckedAt >= now.AddDays(-30) &&
            i.AttemptNumber > 1);

    int previousProblems =
        inspections.Count(i =>
            i.CheckedAt < latestInspection.CheckedAt &&
            !i.IsOk);

    int overdueInspectionsLast30Days =
        serverRoom.ScheduledInspections.Count(s =>
            s.Deadline >= now.AddDays(-30) &&
            s.Deadline <= now &&
            s.Status == "Overdue");

    var latestRepair = serverRoom.ScheduledInspections
        .Where(s => s.FixedAt.HasValue)
        .OrderByDescending(s => s.FixedAt)
        .FirstOrDefault();

    int daysSinceLastRepair;

    if (latestRepair?.FixedAt != null)
    {
        daysSinceLastRepair =
            Math.Max(
                0,
                (int)(now - latestRepair.FixedAt.Value).TotalDays);
    }
    else
    {
        daysSinceLastRepair = 365;
    }

    decimal averageTemperature = inspections
        .Select(i => i.Temperature)
        .DefaultIfEmpty(latestInspection.Temperature)
        .Average();

    decimal temperatureDeviation =
        Math.Abs(
            latestInspection.Temperature -
            averageTemperature);

    var record = new PredictiveMaintenanceRecord
    {
        ServerRoomId = serverRoom.Id,
        RecordedAt = latestInspection.CheckedAt,

        Temperature = latestInspection.Temperature,

        TemperatureDeviation = temperatureDeviation,

        DaysSinceLastInspection =
            daysSinceLastInspection,

        FailedInspectionsLast7Days =
            failedInspectionsLast7Days,

        FailedInspectionsLast30Days =
            failedInspectionsLast30Days,

        FailedAttemptsLast30Days =
            failedAttemptsLast30Days,

        PreviousProblems =
            previousProblems,

        OverdueInspectionsLast30Days =
            overdueInspectionsLast30Days,

        DaysSinceLastRepair =
            daysSinceLastRepair,

        AirConditioningOk =
            latestInspection.AirConditioningOk,

        NoOverheatingAlarm =
            latestInspection.NoOverheatingAlarm,

        NoWaterLeak =
            latestInspection.NoWaterLeak,

        PowerOk =
            latestInspection.PowerOk,

        RoomClean =
            latestInspection.RoomClean,

        FailureWithin7Days = false,

        IsSynthetic = false
    };

    var prediction =
        _predictionService.Predict(record);

    double probability =
        prediction.Probability * 100;

    string riskLevel;

    if (probability >= 70)
    {
        riskLevel = "High";
    }
    else if (probability >= 40)
    {
        riskLevel = "Medium";
    }
    else
    {
        riskLevel = "Low";
    }

    Room = new RoomViewModel
    {
        ServerRoomId = serverRoom.Id,
        RoomName = serverRoom.Name,
        Location = serverRoom.Location,

        HasPrediction = true,

        RecordedAt = latestInspection.CheckedAt,

        Temperature =
            latestInspection.Temperature,

        TemperatureDeviation =
            temperatureDeviation,

        DaysSinceLastInspection =
            daysSinceLastInspection,

        FailedInspectionsLast7Days =
            failedInspectionsLast7Days,

        FailedInspectionsLast30Days =
            failedInspectionsLast30Days,

        FailedAttemptsLast30Days =
            failedAttemptsLast30Days,

        PreviousProblems =
            previousProblems,

        OverdueInspectionsLast30Days =
            overdueInspectionsLast30Days,

        DaysSinceLastRepair =
            daysSinceLastRepair,

        AirConditioningOk =
            latestInspection.AirConditioningOk,

        NoOverheatingAlarm =
            latestInspection.NoOverheatingAlarm,

        NoWaterLeak =
            latestInspection.NoWaterLeak,

        PowerOk =
            latestInspection.PowerOk,

        RoomClean =
            latestInspection.RoomClean,

        Probability =
            probability,

        PredictedFailure =
            prediction.PredictedLabel,

        Score =
            prediction.Score,

        RiskLevel =
            riskLevel,

        IsSynthetic = false
    };

    foreach (var inspection in inspections.Take(20))
    {
        History.Add(
            new HistoryViewModel
            {
                RecordedAt = inspection.CheckedAt,
                Temperature = inspection.Temperature,
                Probability = 0,
                PredictedFailure = !inspection.IsOk,
                ActualFailure = !inspection.IsOk
            });
    }
}


}

public class RoomViewModel
{
public int ServerRoomId { get; set; }


public string RoomName { get; set; } = "";

public string Location { get; set; } = "";

public bool HasPrediction { get; set; }

public DateTime RecordedAt { get; set; }

public decimal Temperature { get; set; }

public decimal TemperatureDeviation { get; set; }

public int DaysSinceLastInspection { get; set; }

public int FailedInspectionsLast7Days { get; set; }

public int FailedInspectionsLast30Days { get; set; }

public int FailedAttemptsLast30Days { get; set; }

public int PreviousProblems { get; set; }

public int OverdueInspectionsLast30Days { get; set; }

public int DaysSinceLastRepair { get; set; }

public bool AirConditioningOk { get; set; }

public bool NoOverheatingAlarm { get; set; }

public bool NoWaterLeak { get; set; }

public bool PowerOk { get; set; }

public bool RoomClean { get; set; }

public double Probability { get; set; }

public bool PredictedFailure { get; set; }

public float Score { get; set; }

public string RiskLevel { get; set; } = "";

public bool IsSynthetic { get; set; }


}

public class HistoryViewModel
{
public DateTime RecordedAt { get; set; }


public decimal Temperature { get; set; }

public double Probability { get; set; }

public bool PredictedFailure { get; set; }

public bool ActualFailure { get; set; }


}
