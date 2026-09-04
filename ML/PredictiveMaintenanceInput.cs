using Microsoft.ML.Data;

namespace ServerRoomMonitor.ML;

public class PredictiveMaintenanceInput
{
    public float Temperature { get; set; }

    public float TemperatureDeviation { get; set; }

    public float DaysSinceLastInspection { get; set; }

    public float FailedInspectionsLast7Days { get; set; }

    public float FailedInspectionsLast30Days { get; set; }

    public float FailedAttemptsLast30Days { get; set; }

    public float PreviousProblems { get; set; }

    public float OverdueInspectionsLast30Days { get; set; }

    public float DaysSinceLastRepair { get; set; }

    public float AirConditioningOk { get; set; }

    public float NoOverheatingAlarm { get; set; }

    public float NoWaterLeak { get; set; }

    public float PowerOk { get; set; }

    public float RoomClean { get; set; }

    [ColumnName("Label")]
    public bool FailureWithin7Days { get; set; }

    // Gives failure records more importance during training.
    public float ExampleWeight { get; set; }
}