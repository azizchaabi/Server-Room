using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServerRoomMonitor.Services;

namespace ServerRoomMonitor.Pages.Admin.Tools;

[Authorize(Roles = "Admin")]
public class GeneratePredictiveDataModel : PageModel
{
private readonly PredictiveDataGeneratorService _generator;


public GeneratePredictiveDataModel(
    PredictiveDataGeneratorService generator)
{
    _generator = generator;
}

public string Message { get; private set; } = "";

public async Task OnGetAsync()
{
    await _generator.GenerateAsync(100);

    Message = "Synthetic predictive maintenance data generated successfully.";
}


}
