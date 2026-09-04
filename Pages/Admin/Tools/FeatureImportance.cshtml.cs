using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServerRoomMonitor.ML;

namespace ServerRoomMonitor.Pages.Admin.Tools;

[Authorize(Roles = "Admin")]
public class FeatureImportanceModel : PageModel
{
private readonly PredictiveMaintenanceModelTuning _tuning;


public FeatureImportanceModel(
    PredictiveMaintenanceModelTuning tuning)
{
    _tuning = tuning;
}

public string Message { get; private set; } = "";

public void OnGet()
{
    try
    {
        _tuning.RunPermutationFeatureImportance();

        Message =
            "Permutation Feature Importance analysis completed successfully. " +
            "Check the application console for the results.";
    }
    catch (Exception ex)
    {
        Message =
            $"Feature importance analysis failed: {ex.Message}";
    }
}


}
