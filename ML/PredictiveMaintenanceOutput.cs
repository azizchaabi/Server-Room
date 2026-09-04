using Microsoft.ML.Data;

namespace ServerRoomMonitor.ML;

public class PredictiveMaintenanceOutput
{
[ColumnName("PredictedLabel")]
public bool FailurePredicted { get; set; }


public float Probability { get; set; }

public float Score { get; set; }


}
