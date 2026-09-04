namespace ServerRoomMonitor.Models;

public class PredictiveMaintenanceRecord
{
public int Id { get; set; }


// Server room associated with this observation.
public int ServerRoomId { get; set; }

// When these conditions were observed.
public DateTime RecordedAt { get; set; }

// Environmental conditions.
public decimal Temperature { get; set; }

// How far the temperature is from the room's normal baseline.
public decimal TemperatureDeviation { get; set; }

// Recent inspection information.
public int DaysSinceLastInspection { get; set; }

public int FailedInspectionsLast7Days { get; set; }

public int FailedInspectionsLast30Days { get; set; }

public int FailedAttemptsLast30Days { get; set; }

// Previous room problems.
public int PreviousProblems { get; set; }

// Scheduling/maintenance indicators.
public int OverdueInspectionsLast30Days { get; set; }

public int DaysSinceLastRepair { get; set; }

// Current inspection condition.
public bool AirConditioningOk { get; set; }

public bool NoOverheatingAlarm { get; set; }

public bool NoWaterLeak { get; set; }

public bool PowerOk { get; set; }

public bool RoomClean { get; set; }

// The value the machine-learning model will learn to predict.
// 0 = no failure within the prediction window.
// 1 = failure within the prediction window.
public bool FailureWithin7Days { get; set; }

// True = generated/test data.
// False = data originating from actual application activity.
public bool IsSynthetic { get; set; }


}
