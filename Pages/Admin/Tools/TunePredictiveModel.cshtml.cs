using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServerRoomMonitor.ML;

namespace ServerRoomMonitor.Pages.Admin.Tools;

[Authorize(Roles = "Admin")]
public class TunePredictiveModelModel : PageModel
{
    private readonly PredictiveMaintenanceModelTuning _tuning;

    public TunePredictiveModelModel(
        PredictiveMaintenanceModelTuning tuning)
    {
        _tuning = tuning;
    }

    public string Message { get; private set; } = "";

    public void OnGet()
    {
        try
        {
            _tuning.RunHyperparameterSearch();

            Message =
                "Hyperparameter search completed successfully. " +
                "Check the application console for the results.";
        }
        catch (Exception ex)
        {
            Message =
                $"Hyperparameter search failed: {ex.Message}";
        }
    }
}