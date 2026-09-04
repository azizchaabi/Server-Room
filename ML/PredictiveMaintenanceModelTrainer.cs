using Microsoft.ML;
using Microsoft.ML.Data;
using ServerRoomMonitor.Data;

namespace ServerRoomMonitor.ML;

public class PredictiveMaintenanceModelTrainer
{
    private readonly ApplicationDbContext _context;

    public PredictiveMaintenanceModelTrainer(ApplicationDbContext context)
    {
        _context = context;
    }

    public void TrainModel()
    {
        var mlContext = new MLContext(seed: 42);

        // Load predictive-maintenance records from SQL Server.
        var records = _context.PredictiveMaintenanceRecords
            .ToList();

        if (records.Count < 100)
        {
            throw new InvalidOperationException(
                "Not enough predictive-maintenance records to train the model.");
        }

        // Convert database records into ML.NET input records.
        var data = records.Select(record => new PredictiveMaintenanceInput
        {
            Temperature = (float)record.Temperature,
            TemperatureDeviation = (float)record.TemperatureDeviation,
            DaysSinceLastInspection = record.DaysSinceLastInspection,
            FailedInspectionsLast7Days = record.FailedInspectionsLast7Days,
            FailedInspectionsLast30Days = record.FailedInspectionsLast30Days,
            FailedAttemptsLast30Days = record.FailedAttemptsLast30Days,
            PreviousProblems = record.PreviousProblems,
            OverdueInspectionsLast30Days = record.OverdueInspectionsLast30Days,
            DaysSinceLastRepair = record.DaysSinceLastRepair,
            AirConditioningOk = record.AirConditioningOk ? 1f : 0f,
            NoOverheatingAlarm = record.NoOverheatingAlarm ? 1f : 0f,
            NoWaterLeak = record.NoWaterLeak ? 1f : 0f,
            PowerOk = record.PowerOk ? 1f : 0f,
            RoomClean = record.RoomClean ? 1f : 0f,
            FailureWithin7Days = record.FailureWithin7Days,

            // Bumped from 2x -> 3x. Tune this: higher values push recall up
            // (catch more real failures) at the cost of more false alarms.
            ExampleWeight = record.FailureWithin7Days ? 3f : 1f
        }).ToList();

        string[] featureColumns =
        {
            nameof(PredictiveMaintenanceInput.Temperature),
            nameof(PredictiveMaintenanceInput.TemperatureDeviation),
            nameof(PredictiveMaintenanceInput.DaysSinceLastInspection),
            nameof(PredictiveMaintenanceInput.FailedInspectionsLast7Days),
            nameof(PredictiveMaintenanceInput.FailedInspectionsLast30Days),
            nameof(PredictiveMaintenanceInput.FailedAttemptsLast30Days),
            nameof(PredictiveMaintenanceInput.PreviousProblems),
            nameof(PredictiveMaintenanceInput.OverdueInspectionsLast30Days),
            nameof(PredictiveMaintenanceInput.DaysSinceLastRepair),
            nameof(PredictiveMaintenanceInput.AirConditioningOk),
            nameof(PredictiveMaintenanceInput.NoOverheatingAlarm),
            nameof(PredictiveMaintenanceInput.NoWaterLeak),
            nameof(PredictiveMaintenanceInput.PowerOk),
            nameof(PredictiveMaintenanceInput.RoomClean)
        };

        // LightGBM generally beats FastForest on tabular data like this and
        // gives you calibrated probabilities to work with (needs the
        // Microsoft.ML.LightGbm NuGet package).
        var pipeline =
            mlContext.Transforms
                .Concatenate("Features", featureColumns)
                .Append(
                    mlContext.BinaryClassification.Trainers.LightGbm(
                        labelColumnName: "Label",
                        featureColumnName: "Features",
                        exampleWeightColumnName: "ExampleWeight",
                        numberOfLeaves: 15,
                        minimumExampleCountPerLeaf: 10,
                        learningRate: 0.05,
                        numberOfIterations: 200));

        IDataView fullData = mlContext.Data.LoadFromEnumerable(data);

        // --------------------------------------------------
        // 5-fold cross-validation. With ~1,200 rows (and only a couple
        // hundred positives), a single 80/20 split is noisy — CV gives a
        // much more trustworthy read on how the model actually performs
        // before you commit to hyperparameters.
        // --------------------------------------------------
        var cvResults = mlContext.BinaryClassification.CrossValidateNonCalibrated(
            fullData, pipeline, numberOfFolds: 5, labelColumnName: "Label");

        Console.WriteLine("======================================");
        Console.WriteLine("5-Fold Cross-Validation (whole dataset)");
        Console.WriteLine("======================================");
        Console.WriteLine($"Avg Accuracy:  {cvResults.Average(r => r.Metrics.Accuracy):P2}");
        Console.WriteLine($"Avg Precision: {cvResults.Average(r => r.Metrics.PositivePrecision):P2}");
        Console.WriteLine($"Avg Recall:    {cvResults.Average(r => r.Metrics.PositiveRecall):P2}");
        Console.WriteLine($"Avg F1:        {cvResults.Average(r => r.Metrics.F1Score):P2}");
        Console.WriteLine($"Avg AUC:       {cvResults.Average(r => r.Metrics.AreaUnderRocCurve):P2}");

        // --------------------------------------------------
        // Stratified 80/20 holdout split, kept for a final sanity check and
        // for the threshold sweep below.
        // --------------------------------------------------
        var random = new Random(42);

        var positiveRecords = data
            .Where(x => x.FailureWithin7Days)
            .OrderBy(_ => random.Next())
            .ToList();

        var negativeRecords = data
            .Where(x => !x.FailureWithin7Days)
            .OrderBy(_ => random.Next())
            .ToList();

        int positiveTrainingCount = (int)(positiveRecords.Count * 0.80);
        int negativeTrainingCount = (int)(negativeRecords.Count * 0.80);

        var trainingRecords =
            positiveRecords.Take(positiveTrainingCount)
                .Concat(negativeRecords.Take(negativeTrainingCount))
                .OrderBy(_ => random.Next())
                .ToList();

        var testRecords =
            positiveRecords.Skip(positiveTrainingCount)
                .Concat(negativeRecords.Skip(negativeTrainingCount))
                .OrderBy(_ => random.Next())
                .ToList();

        IDataView trainingData = mlContext.Data.LoadFromEnumerable(trainingRecords);
        IDataView testData = mlContext.Data.LoadFromEnumerable(testRecords);

        var model = pipeline.Fit(trainingData);
        var predictions = model.Transform(testData);

        var metrics =
            mlContext.BinaryClassification.EvaluateNonCalibrated(
                predictions,
                labelColumnName: "Label",
                scoreColumnName: "Score",
                predictedLabelColumnName: "PredictedLabel");

        Console.WriteLine();
        Console.WriteLine("======================================");
        Console.WriteLine("Holdout Test Set");
        Console.WriteLine("======================================");
        Console.WriteLine($"Records:   {records.Count}");
        Console.WriteLine($"Training:  {trainingRecords.Count}");
        Console.WriteLine($"Testing:   {testRecords.Count}");
        Console.WriteLine($"Accuracy:  {metrics.Accuracy:P2}");
        Console.WriteLine($"Precision: {metrics.PositivePrecision:P2}");
        Console.WriteLine($"Recall:    {metrics.PositiveRecall:P2}");
        Console.WriteLine($"F1 Score:  {metrics.F1Score:P2}");
        Console.WriteLine($"AUC:       {metrics.AreaUnderRocCurve:P2}");
        Console.WriteLine();

        // FIX: the original code hand-indexed matrix.Counts[1][1] etc. and
        // labeled them TP/FP/FN/TN. ML.NET does NOT guarantee that ordering
        // matches true/false, and in the original results it was reversed
        // for the diagonal (TP/TN were swapped). Use the library's own
        // formatter instead of guessing the index order.
        Console.WriteLine(metrics.ConfusionMatrix.GetFormattedConfusionTable());

        // --------------------------------------------------
        // Threshold sweep. A missed real failure (false negative) is
        // usually far more costly than a false alarm, so don't assume the
        // default cutoff is right for you — pick the threshold that fits
        // your actual tolerance.
        // --------------------------------------------------
        Console.WriteLine();
        Console.WriteLine("======================================");
        Console.WriteLine("Threshold sweep (holdout set, on raw Score)");
        Console.WriteLine("======================================");

        var scoredRows = mlContext.Data
            .CreateEnumerable<PredictiveMaintenanceScoredRow>(predictions, reuseRowObject: false)
            .ToList();

        foreach (var threshold in new[] { -1.5f, -1f, -0.5f, 0f, 0.5f, 1f, 1.5f, 2f })
        {
            int tp = scoredRows.Count(r => r.Label && r.Score >= threshold);
            int fp = scoredRows.Count(r => !r.Label && r.Score >= threshold);
            int fn = scoredRows.Count(r => r.Label && r.Score < threshold);
            int tn = scoredRows.Count(r => !r.Label && r.Score < threshold);

            double precision = tp + fp == 0 ? 0 : (double)tp / (tp + fp);
            double recall = tp + fn == 0 ? 0 : (double)tp / (tp + fn);

            Console.WriteLine(
                $"Threshold {threshold,5:0.0}: Precision {precision,6:P1}  Recall {recall,6:P1}  " +
                $"TP {tp,3}  FP {fp,3}  FN {fn,3}  TN {tn,3}");
        }

        Console.WriteLine("======================================");

        // --------------------------------------------------
        // Once you're happy with the CV + holdout numbers above, retrain on
        // ALL the data for the model you actually deploy.
        // --------------------------------------------------
        var finalModel = pipeline.Fit(fullData);

        string modelDirectory =
            Path.Combine(AppContext.BaseDirectory, "MLModels");

        Directory.CreateDirectory(modelDirectory);

        string modelPath =
            Path.Combine(modelDirectory, "PredictiveMaintenanceModel.zip");

        mlContext.Model.Save(finalModel, fullData.Schema, modelPath);

        Console.WriteLine();
        Console.WriteLine($"Model saved to: {modelPath}");
    }
}

// Minimal shape used only to read Label/Score back out for the threshold sweep.
public class PredictiveMaintenanceScoredRow
{
    public bool Label { get; set; }
    public float Score { get; set; }
}