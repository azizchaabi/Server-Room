using Microsoft.ML;
using ServerRoomMonitor.Data;

namespace ServerRoomMonitor.ML;

/// <summary>
/// Diagnostic tools for tuning and understanding the predictive
/// maintenance model.
/// </summary>
public class PredictiveMaintenanceModelTuning
{
private readonly ApplicationDbContext _context;


public PredictiveMaintenanceModelTuning(ApplicationDbContext context)
{
    _context = context;
}

public void RunHyperparameterSearch()
{
    var mlContext = new MLContext(seed: 42);

    var records = _context.PredictiveMaintenanceRecords.ToList();

    if (records.Count < 100)
        throw new InvalidOperationException(
            "Not enough predictive-maintenance records to run tuning.");

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
        ExampleWeight = 1f
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

    var leafOptions = new[] { 15, 31, 63 };
    var learningRateOptions = new[] { 0.02, 0.05, 0.1 };
    var iterationOptions = new[] { 100, 200 };
    var weightOptions = new[] { 2f, 3f, 4f, 5f };

    Console.WriteLine("======================================");
    Console.WriteLine("Hyperparameter grid search (5-fold CV)");
    Console.WriteLine("======================================");

    Console.WriteLine(
        $"{"Leaves",7} {"LR",6} {"Iters",6} {"Weight",6} | " +
        $"{"AUC",6} {"Prec",6} {"Recall",7} {"F1",6}");

    var results =
        new List<(
            int leaves,
            double lr,
            int iters,
            float weight,
            double auc,
            double precision,
            double recall,
            double f1)>();

    foreach (var leaves in leafOptions)
    foreach (var lr in learningRateOptions)
    foreach (var iters in iterationOptions)
    foreach (var weight in weightOptions)
    {
        var weightedData = data.Select(d => new PredictiveMaintenanceInput
        {
            Temperature = d.Temperature,
            TemperatureDeviation = d.TemperatureDeviation,
            DaysSinceLastInspection = d.DaysSinceLastInspection,
            FailedInspectionsLast7Days = d.FailedInspectionsLast7Days,
            FailedInspectionsLast30Days = d.FailedInspectionsLast30Days,
            FailedAttemptsLast30Days = d.FailedAttemptsLast30Days,
            PreviousProblems = d.PreviousProblems,
            OverdueInspectionsLast30Days = d.OverdueInspectionsLast30Days,
            DaysSinceLastRepair = d.DaysSinceLastRepair,
            AirConditioningOk = d.AirConditioningOk,
            NoOverheatingAlarm = d.NoOverheatingAlarm,
            NoWaterLeak = d.NoWaterLeak,
            PowerOk = d.PowerOk,
            RoomClean = d.RoomClean,
            FailureWithin7Days = d.FailureWithin7Days,
            ExampleWeight = d.FailureWithin7Days ? weight : 1f
        }).ToList();

        IDataView fullData =
            mlContext.Data.LoadFromEnumerable(weightedData);

        var pipeline = mlContext.Transforms
            .Concatenate("Features", featureColumns)
            .Append(
                mlContext.BinaryClassification.Trainers.LightGbm(
                    labelColumnName: "Label",
                    featureColumnName: "Features",
                    exampleWeightColumnName: "ExampleWeight",
                    numberOfLeaves: leaves,
                    minimumExampleCountPerLeaf: 10,
                    learningRate: lr,
                    numberOfIterations: iters));

        var cvResults =
            mlContext.BinaryClassification.CrossValidateNonCalibrated(
                fullData,
                pipeline,
                numberOfFolds: 5,
                labelColumnName: "Label");

        double avgAuc =
            cvResults.Average(r => r.Metrics.AreaUnderRocCurve);

        double avgPrecision =
            cvResults.Average(r => r.Metrics.PositivePrecision);

        double avgRecall =
            cvResults.Average(r => r.Metrics.PositiveRecall);

        double avgF1 =
            cvResults.Average(r => r.Metrics.F1Score);

        results.Add(
            (
                leaves,
                lr,
                iters,
                weight,
                avgAuc,
                avgPrecision,
                avgRecall,
                avgF1
            ));

        Console.WriteLine(
            $"{leaves,7} {lr,6:0.00} {iters,6} {weight,6:0.0} | " +
            $"{avgAuc,6:P0} {avgPrecision,6:P0} " +
            $"{avgRecall,7:P0} {avgF1,6:P0}");
    }

    Console.WriteLine();
    Console.WriteLine("Top 5 by AUC (best overall ranking ability):");

    foreach (var r in results.OrderByDescending(r => r.auc).Take(5))
    {
        Console.WriteLine(
            $"  leaves={r.leaves} lr={r.lr} iters={r.iters} " +
            $"weight={r.weight} => " +
            $"AUC {r.auc:P2}  " +
            $"Precision {r.precision:P2}  " +
            $"Recall {r.recall:P2}  " +
            $"F1 {r.f1:P2}");
    }

    Console.WriteLine();
    Console.WriteLine("Top 5 by Recall (fewest missed failures):");

    foreach (var r in results.OrderByDescending(r => r.recall).Take(5))
    {
        Console.WriteLine(
            $"  leaves={r.leaves} lr={r.lr} iters={r.iters} " +
            $"weight={r.weight} => " +
            $"AUC {r.auc:P2}  " +
            $"Precision {r.precision:P2}  " +
            $"Recall {r.recall:P2}  " +
            $"F1 {r.f1:P2}");
    }

    Console.WriteLine("======================================");
}

public void RunPermutationFeatureImportance()
{
    var mlContext = new MLContext(seed: 42);

    var records = _context.PredictiveMaintenanceRecords.ToList();

    if (records.Count < 100)
        throw new InvalidOperationException(
            "Not enough predictive-maintenance records to run feature importance.");

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
        ExampleWeight = record.FailureWithin7Days ? 4f : 1f
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

    // --------------------------------------
    // Stratified 80/20 split
    // --------------------------------------

    var random = new Random(42);

    var positiveRecords = data
        .Where(x => x.FailureWithin7Days)
        .OrderBy(_ => random.Next())
        .ToList();

    var negativeRecords = data
        .Where(x => !x.FailureWithin7Days)
        .OrderBy(_ => random.Next())
        .ToList();

    int positiveTrainingCount =
        (int)(positiveRecords.Count * 0.80);

    int negativeTrainingCount =
        (int)(negativeRecords.Count * 0.80);

    var trainingRecords =
        positiveRecords
            .Take(positiveTrainingCount)
            .Concat(
                negativeRecords.Take(negativeTrainingCount))
            .OrderBy(_ => random.Next())
            .ToList();

    var testRecords =
        positiveRecords
            .Skip(positiveTrainingCount)
            .Concat(
                negativeRecords.Skip(negativeTrainingCount))
            .OrderBy(_ => random.Next())
            .ToList();

    IDataView trainingData =
        mlContext.Data.LoadFromEnumerable(trainingRecords);

    IDataView testData =
        mlContext.Data.LoadFromEnumerable(testRecords);

    // --------------------------------------
    // Tuned LightGBM model
    // --------------------------------------
    //
    // Best configuration from the previous
    // hyperparameter search by AUC:
    //
    // Leaves:       15
    // LearningRate: 0.02
    // Iterations:   100
    // PositiveWeight: 4
    //

    var pipeline = mlContext.Transforms
        .Concatenate("Features", featureColumns)
        .Append(
            mlContext.BinaryClassification.Trainers.LightGbm(
                labelColumnName: "Label",
                featureColumnName: "Features",
                exampleWeightColumnName: "ExampleWeight",
                numberOfLeaves: 15,
                minimumExampleCountPerLeaf: 10,
                learningRate: 0.02,
                numberOfIterations: 100));

    var model = pipeline.Fit(trainingData);

    // --------------------------------------
    // Run PFI on unseen test data
    // --------------------------------------

    var transformedTestData =
        model.Transform(testData);

    var pfi =
        mlContext.BinaryClassification
            .PermutationFeatureImportanceNonCalibrated(
                model,
                transformedTestData,
                labelColumnName: "Label",
                permutationCount: 10);

    Console.WriteLine();
    Console.WriteLine("======================================");
    Console.WriteLine("Permutation Feature Importance");
    Console.WriteLine("======================================");
    Console.WriteLine(
        "PFI is calculated on the unseen 20% test set.");
    Console.WriteLine(
        "A larger positive AUC drop means the feature is more important.");
    Console.WriteLine(
        "A value near zero means the feature has little measurable impact.");
    Console.WriteLine();

    var ranked = pfi
        .Select(x => new
        {
            Feature = x.Key,
            AucDrop = x.Value.AreaUnderRocCurve.Mean
        })
        .OrderByDescending(x => x.AucDrop);

    foreach (var item in ranked)
    {
        Console.WriteLine(
            $"{item.Feature,-32} {item.AucDrop,8:0.0000}");
    }

    Console.WriteLine("======================================");
    Console.WriteLine();

    Console.WriteLine(
        $"Training records used: {trainingRecords.Count}");

    Console.WriteLine(
        $"Unseen test records used: {testRecords.Count}");

    Console.WriteLine(
        $"Positive test records: {testRecords.Count(x => x.FailureWithin7Days)}");

    Console.WriteLine(
        $"Negative test records: {testRecords.Count(x => !x.FailureWithin7Days)}");

    Console.WriteLine("======================================");
}


}
