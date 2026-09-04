using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServerRoomMonitor.ML;

namespace ServerRoomMonitor.Pages.Admin.Tools;

[Authorize(Roles = "Admin")]
public class TrainPredictiveModelModel : PageModel
{
private readonly PredictiveMaintenanceModelTrainer _trainer;


public TrainPredictiveModelModel(
    PredictiveMaintenanceModelTrainer trainer)
{
    _trainer = trainer;
}

public string Message { get; private set; } = "";

public void OnGet()
{
    try
    {
        _trainer.TrainModel();

        Message = "Predictive maintenance model trained successfully. Check the application console for the evaluation results.";
    }
    catch (Exception ex)
    {
        Message = $"Model training failed: {ex.Message}";
    }
}


}
