using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ServerRoomMonitor.Data;
using ServerRoomMonitor.ML;

namespace ServerRoomMonitor.Pages.Admin.PredictiveMaintenance;

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

    public List<PredictionViewModel> Predictions { get; set; } = new();

    public int TotalPredictions { get; set; }

    public int HighRiskCount { get; set; }

    public int LowRiskCount { get; set; }

    public bool ModelAvailable { get; set; }

    public async Task OnGetAsync()
    {
        var rooms = await _context.ServerRooms
            .Include(r => r.Inspections)
            .Include(r => r.ScheduledInspections)
            .ToListAsync();

        foreach (var room in rooms)
        {
            var inspections = room.Inspections
                .OrderByDescending(i => i.CheckedAt)
                .ToList();

            if (inspections.Count == 0)
            {
                continue;
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
                room.ScheduledInspections.Count(s =>
                    s.Deadline >= now.AddDays(-30) &&
                    s.Deadline <= now &&
                    s.Status == "Overdue");

            var latestRepair = room.ScheduledInspections
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

            var record = new Models.PredictiveMaintenanceRecord
            {
                ServerRoomId = room.Id,

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

                FailureWithin7Days = false,

                IsSynthetic = false
            };

            try
            {
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

                Predictions.Add(
                    new PredictionViewModel
                    {
                        ServerRoomId = room.Id,


                        RoomName = room.Name,

                        Location = room.Location,

                        Temperature =
                            latestInspection.Temperature,



                        RecordedAt =
                            latestInspection.CheckedAt,

                        Probability =
                            probability,

                        PredictedFailure =
                            prediction.PredictedLabel,

                        RiskLevel =
                            riskLevel,

                        IsSynthetic = false
                    });

                ModelAvailable = true;
            }
            catch
            {
                // Ignore rooms where a prediction cannot be generated.
            }
        }

        Predictions = Predictions
            .OrderByDescending(p => p.Probability)
            .ToList();

        TotalPredictions =
            Predictions.Count;

        HighRiskCount =
            Predictions.Count(p =>
                p.RiskLevel == "High");

        LowRiskCount =
            Predictions.Count(p =>
                p.RiskLevel == "Low");
    }


}

public class PredictionViewModel
{
    public int ServerRoomId { get; set; }

    public string RoomName { get; set; } = "";

    public string Location { get; set; } = "";

    public decimal Temperature { get; set; }



    public DateTime RecordedAt { get; set; }

    public double Probability { get; set; }

    public bool PredictedFailure { get; set; }

    public string RiskLevel { get; set; } = "";

    public bool IsSynthetic { get; set; }


}
