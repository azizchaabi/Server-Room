using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServerRoomMonitor.Data;
using ServerRoomMonitor.ML;

namespace ServerRoomMonitor.Pages.Admin.Tools;

[Authorize(Roles = "Admin")]
public class TestPredictiveModelModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly PredictiveMaintenancePredictionService _predictionService;

    public TestPredictiveModelModel(
        ApplicationDbContext context,
        PredictiveMaintenancePredictionService predictionService)
    {
        _context = context;
        _predictionService = predictionService;
    }

    public string Result { get; private set; } = "";

    public void OnGet()
    {
        var record = _context.PredictiveMaintenanceRecords
            .OrderByDescending(r => r.RecordedAt)
            .FirstOrDefault();

        if (record == null)
        {
            Result = "No predictive-maintenance records found.";
            return;
        }

        try
        {
            var prediction =
                _predictionService.Predict(record);

            Result =
                $"Server Room ID: {record.ServerRoomId}\n" +
                $"Recorded At: {record.RecordedAt}\n" +
                $"Temperature: {record.Temperature:F2} °C\n" +
                $"Actual Failure Label: {record.FailureWithin7Days}\n" +
                $"Predicted Failure: {prediction.PredictedLabel}\n" +
                $"Score: {prediction.Score:F4}\n" +
                $"Probability: {prediction.Probability:P2}";
        }
        catch (Exception ex)
        {
            Result =
                $"Prediction failed:\n{ex}";
        }
    }
}
