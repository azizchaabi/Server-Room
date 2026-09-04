using Microsoft.ML;
using ServerRoomMonitor.Models;

namespace ServerRoomMonitor.ML;

public class PredictiveMaintenancePredictionService
{
    private readonly MLContext _mlContext;
    private readonly PredictionEngine<PredictiveMaintenanceInput, PredictiveMaintenancePrediction> _predictionEngine;

    public PredictiveMaintenancePredictionService()
    {
        _mlContext = new MLContext(seed: 42);

        string modelPath = Path.Combine(
            AppContext.BaseDirectory,
            "MLModels",
            "PredictiveMaintenanceModel.zip");

        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException(
                "Predictive maintenance model was not found.",
                modelPath);
        }

        ITransformer model =
            _mlContext.Model.Load(
                modelPath,
                out _);

        _predictionEngine =
            _mlContext.Model.CreatePredictionEngine<
                PredictiveMaintenanceInput,
                PredictiveMaintenancePrediction>(
                    model);
    }

    public PredictiveMaintenancePrediction Predict(
        PredictiveMaintenanceRecord record)
    {
        var input = new PredictiveMaintenanceInput
        {
            Temperature =
                (float)record.Temperature,

            TemperatureDeviation =
                (float)record.TemperatureDeviation,

            DaysSinceLastInspection =
                record.DaysSinceLastInspection,

            FailedInspectionsLast7Days =
                record.FailedInspectionsLast7Days,

            FailedInspectionsLast30Days =
                record.FailedInspectionsLast30Days,

            FailedAttemptsLast30Days =
                record.FailedAttemptsLast30Days,

            PreviousProblems =
                record.PreviousProblems,

            OverdueInspectionsLast30Days =
                record.OverdueInspectionsLast30Days,

            DaysSinceLastRepair =
                record.DaysSinceLastRepair,

            AirConditioningOk =
                record.AirConditioningOk ? 1f : 0f,

            NoOverheatingAlarm =
                record.NoOverheatingAlarm ? 1f : 0f,

            NoWaterLeak =
                record.NoWaterLeak ? 1f : 0f,

            PowerOk =
                record.PowerOk ? 1f : 0f,

            RoomClean =
                record.RoomClean ? 1f : 0f,

            // This field is ignored when making a prediction.
            // It exists because the input class is also used for training.
            FailureWithin7Days =
                false,

            ExampleWeight =
                1f
        };

        return _predictionEngine.Predict(input);
    }
}

public class PredictiveMaintenancePrediction
{
    public bool PredictedLabel { get; set; }

    public float Score { get; set; }

    public float Probability { get; set; }
}

