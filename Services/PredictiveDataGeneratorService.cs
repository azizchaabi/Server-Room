using ServerRoomMonitor.Data;
using ServerRoomMonitor.Models;

namespace ServerRoomMonitor.Services;

public class PredictiveDataGeneratorService
{
    private readonly ApplicationDbContext _context;
    private readonly Random _random = new(42);

    public PredictiveDataGeneratorService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task GenerateAsync(int recordsPerRoom = 100)
    {
        var rooms = _context.ServerRooms.ToList();

        if (rooms.Count == 0)
            return;

        var records = new List<PredictiveMaintenanceRecord>();

        foreach (var room in rooms)
        {
            // Each room starts in a different condition.
            // This gives the dataset a mixture of healthy,
            // deteriorating and already-problematic rooms.
            double healthState = _random.Next(20, 86);

            // Give each room a general long-term tendency.
            // Positive = tends to deteriorate.
            // Negative = tends to recover.
            double roomTrend = _random.NextDouble() switch
            {
                < 0.20 => _random.NextDouble() * 0.30 + 0.10,
                < 0.80 => (_random.NextDouble() - 0.5) * 0.20,
                _ => -(_random.NextDouble() * 0.25 + 0.05)
            };

            for (int i = 0; i < recordsPerRoom; i++)
            {
                var recordedAt =
                    DateTime.Now
                        .Date
                        .AddDays(-recordsPerRoom + i);

                // --------------------------------------------------
                // Health evolution
                // --------------------------------------------------

                healthState += roomTrend;

                // Normal day-to-day variation.
                healthState +=
                    (_random.NextDouble() - 0.5) * 5.0;

                // Occasional deterioration event.
                if (_random.NextDouble() < 0.08)
                {
                    healthState +=
                        _random.NextDouble() * 8.0 + 2.0;
                }

                // Occasional maintenance/recovery event.
                if (_random.NextDouble() < 0.06)
                {
                    healthState -=
                        _random.NextDouble() * 7.0 + 2.0;
                }

                healthState =
                    Math.Clamp(
                        healthState,
                        0,
                        100);

                // --------------------------------------------------
                // Temperature
                // --------------------------------------------------

                decimal temperature;

                if (healthState < 45)
                {
                    temperature =
                        20m +
                        (decimal)_random.NextDouble() * 3m;
                }
                else if (healthState < 65)
                {
                    temperature =
                        22m +
                        (decimal)_random.NextDouble() * 4m;
                }
                else if (healthState < 82)
                {
                    temperature =
                        25m +
                        (decimal)_random.NextDouble() * 4m;
                }
                else if (healthState < 93)
                {
                    temperature =
                        28m +
                        (decimal)_random.NextDouble() * 4m;
                }
                else
                {
                    temperature =
                        31m +
                        (decimal)_random.NextDouble() * 5m;
                }

                temperature =
                    Math.Round(
                        temperature,
                        2);

                decimal normalTemperature = 22m;

                decimal temperatureDeviation =
                    Math.Abs(
                        temperature -
                        normalTemperature);

                // --------------------------------------------------
                // Inspection history
                // --------------------------------------------------

                int daysSinceInspection =
                    Math.Max(
                        1,
                        (int)Math.Round(
                            2 +
                            healthState * 0.12 +
                            (_random.NextDouble() - 0.5) * 5));

                int failedInspectionsLast7Days =
                    healthState < 45
                        ? _random.Next(0, 2)
                        : healthState < 65
                            ? _random.Next(0, 3)
                            : healthState < 82
                                ? _random.Next(1, 4)
                                : _random.Next(2, 6);

                int failedInspectionsLast30Days =
                    failedInspectionsLast7Days +
                    (
                        healthState < 45
                            ? _random.Next(0, 2)
                            : healthState < 65
                                ? _random.Next(0, 3)
                                : healthState < 82
                                    ? _random.Next(1, 4)
                                    : _random.Next(2, 6)
                    );

                int failedAttemptsLast30Days =
                    healthState < 45
                        ? _random.Next(0, 2)
                        : healthState < 65
                            ? _random.Next(0, 3)
                            : healthState < 82
                                ? _random.Next(1, 4)
                                : _random.Next(2, 6);

                int previousProblems =
                    healthState < 45
                        ? _random.Next(0, 2)
                        : healthState < 65
                            ? _random.Next(0, 3)
                            : healthState < 82
                                ? _random.Next(1, 5)
                                : _random.Next(3, 7);

                int overdueInspectionsLast30Days =
                    healthState < 45
                        ? _random.Next(0, 2)
                        : healthState < 65
                            ? _random.Next(0, 3)
                            : healthState < 82
                                ? _random.Next(1, 4)
                                : _random.Next(2, 6);

                // --------------------------------------------------
                // Repair history
                // --------------------------------------------------

                int daysSinceLastRepair =
                    healthState < 45
                        ? _random.Next(1, 31)
                        : healthState < 65
                            ? _random.Next(10, 46)
                            : healthState < 82
                                ? _random.Next(25, 71)
                                : _random.Next(45, 121);

                // --------------------------------------------------
                // Room conditions
                // --------------------------------------------------

                double conditionProbability =
                    Math.Clamp(
                        0.98 -
                        healthState * 0.0045,
                        0.50,
                        0.98);

                bool airConditioningOk =
                    _random.NextDouble() <
                    conditionProbability;

                bool noOverheatingAlarm =
                    _random.NextDouble() <
                    Math.Clamp(
                        conditionProbability -
                        0.02,
                        0.45,
                        0.97);

                bool noWaterLeak =
                    _random.NextDouble() <
                    Math.Clamp(
                        conditionProbability +
                        0.01,
                        0.55,
                        0.99);

                bool powerOk =
                    _random.NextDouble() <
                    Math.Clamp(
                        conditionProbability -
                        0.01,
                        0.45,
                        0.98);

                bool roomClean =
                    _random.NextDouble() <
                    Math.Clamp(
                        conditionProbability +
                        0.01,
                        0.55,
                        0.99);

                // --------------------------------------------------
                // Failure probability
                // --------------------------------------------------

                double risk;

                if (healthState < 45)
                {
                    risk = 0.005;
                }
                else if (healthState < 65)
                {
                    risk = 0.025;
                }
                else if (healthState < 82)
                {
                    risk = 0.08;
                }
                else if (healthState < 93)
                {
                    risk = 0.18;
                }
                else
                {
                    risk = 0.32;
                }

                // Temperature contribution.
                if (temperature > 25)
                    risk += 0.05;

                if (temperature > 28)
                    risk += 0.08;

                if (temperature > 30)
                    risk += 0.12;

                if (temperatureDeviation > 5)
                    risk += 0.04;

                // Inspection history.
                risk +=
                    failedInspectionsLast7Days *
                    0.035;

                risk +=
                    failedInspectionsLast30Days *
                    0.012;

                risk +=
                    failedAttemptsLast30Days *
                    0.018;

                risk +=
                    previousProblems *
                    0.012;

                risk +=
                    overdueInspectionsLast30Days *
                    0.018;

                // Repair age.
                if (daysSinceLastRepair > 30)
                    risk += 0.035;

                if (daysSinceLastRepair > 60)
                    risk += 0.06;

                if (daysSinceLastRepair > 90)
                    risk += 0.08;

                // Room conditions.
                if (!airConditioningOk)
                    risk += 0.10;

                if (!noOverheatingAlarm)
                    risk += 0.14;

                if (!noWaterLeak)
                    risk += 0.08;

                if (!powerOk)
                    risk += 0.14;

                if (!roomClean)
                    risk += 0.025;

                // Inspection recency.
                if (daysSinceInspection > 10)
                    risk += 0.04;

                if (daysSinceInspection > 15)
                    risk += 0.05;

                // Small amount of randomness prevents the model
                // from learning a perfectly deterministic rule.
                risk +=
                    (_random.NextDouble() - 0.5) *
                    0.03;

                risk =
                    Math.Clamp(
                        risk,
                        0.002,
                        0.80);

                bool failureWithin7Days =
                    _random.NextDouble() <
                    risk;

                records.Add(
                    new PredictiveMaintenanceRecord
                    {
                        ServerRoomId =
                            room.Id,

                        RecordedAt =
                            recordedAt,

                        Temperature =
                            temperature,

                        TemperatureDeviation =
                            temperatureDeviation,

                        DaysSinceLastInspection =
                            daysSinceInspection,

                        FailedInspectionsLast7Days =
                            failedInspectionsLast7Days,

                        FailedInspectionsLast30Days =
                            failedInspectionsLast30Days,

                        FailedAttemptsLast30Days =
                            failedAttemptsLast30Days,

                        PreviousProblems =
                            previousProblems,

                        OverdueInspectionsLast30Days =
                            overdueInspectionsLast30Days,

                        DaysSinceLastRepair =
                            daysSinceLastRepair,

                        AirConditioningOk =
                            airConditioningOk,

                        NoOverheatingAlarm =
                            noOverheatingAlarm,

                        NoWaterLeak =
                            noWaterLeak,

                        PowerOk =
                            powerOk,

                        RoomClean =
                            roomClean,

                        FailureWithin7Days =
                            failureWithin7Days,

                        IsSynthetic =
                            true
                    });
            }
        }

        await _context.PredictiveMaintenanceRecords
            .AddRangeAsync(records);

        await _context.SaveChangesAsync();
    }
}
