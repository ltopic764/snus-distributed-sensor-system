using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SNUSSensorSystem.ConsensusService.Algorithms;
using SNUSSensorSystem.ConsensusService.Data;
using SNUSSensorSystem.Shared.Enums;
using SNUSSensorSystem.Shared.Models;

namespace SNUSSensorSystem.ConsensusService.Services;

public class ConsensusCalculatorService
    : IConsensusCalculatorService
{
    private readonly ConsensusDbContext _dbContext;
    private readonly IBftConsensusAlgorithm _algorithm;
    private readonly ConsensusOptions _options;

    private readonly ILogger<ConsensusCalculatorService> _logger;

    public ConsensusCalculatorService(
        ConsensusDbContext dbContext,
        IBftConsensusAlgorithm algorithm,
        IOptions<ConsensusOptions> options,
        ILogger<ConsensusCalculatorService> logger)
    {
        _dbContext = dbContext;
        _algorithm = algorithm;
        _options = options.Value;
        _logger = logger;
    }

    public async Task CalculateForPreviousMinuteAsync(
        DateTime currentUtcTime,
        CancellationToken cancellationToken)
    {
        var currentMinuteStart =
            TruncateToMinute(currentUtcTime);

        var previousMinuteStart =
            currentMinuteStart.AddMinutes(-1);

        var previousMinuteEnd = currentMinuteStart;

        /*
         * Prevents the same minute from being processed multiple times,
         * e.g., after a worker restart
         */
        var consensusAlreadyExists =
            await _dbContext.ConsensusValues
                .AsNoTracking()
                .AnyAsync(
                    value =>
                        value.Timestamp ==
                        previousMinuteStart,
                    cancellationToken);

        if (consensusAlreadyExists)
        {
            _logger.LogDebug(
                "Consensus for minute {Minute} already exists.",
                previousMinuteStart);

            return;
        }

        /*
         * We load only raw readings from the previous
         * completed minute
         */
        var readings = await _dbContext.SensorReadings
            .AsNoTracking()
            .Where(reading =>
                reading.Timestamp >= previousMinuteStart &&
                reading.Timestamp < previousMinuteEnd &&
                reading.DataQuality == DataQuality.Good &&
                !reading.IsConsensus)
            .ToListAsync(cancellationToken);

        if (readings.Count == 0)
        {
            _logger.LogWarning(
                "No GOOD readings found between {Start} and {End}.",
                previousMinuteStart,
                previousMinuteEnd);

            return;
        }

        /*
         * Additionally, we check if the sensor itself
         * is currently marked as GOOD
         */
        var sensorIds = readings
            .Select(reading => reading.SensorId)
            .Distinct()
            .ToArray();

        var goodSensorIds = await _dbContext.Sensors
            .AsNoTracking()
            .Where(sensor =>
                sensorIds.Contains(sensor.Id) &&
                sensor.DataQuality == DataQuality.Good)
            .Select(sensor => sensor.Id)
            .ToListAsync(cancellationToken);

        var filteredReadings = readings
            .Where(reading =>
                goodSensorIds.Contains(reading.SensorId))
            .ToArray();

        var result = _algorithm.Calculate(filteredReadings);

        if (!result.Success ||
            result.ConsensusValue is null)
        {
            _logger.LogWarning(
                "Consensus calculation failed for minute " +
                "{Minute}. Reason: {Reason}",
                previousMinuteStart,
                result.FailureReason);

            return;
        }

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            var consensusValue = new ConsensusValue
            {
                Value = result.ConsensusValue.Value,

                // Minute for which the consensus was calculated
                Timestamp = previousMinuteStart,

                CalculatedAt = DateTime.UtcNow,

                ParticipatingSensorCount =
                    result.ParticipatingSensorIds.Count,

                IsConsensus = true
            };

            _dbContext.ConsensusValues.Add(consensusValue);

            if (_options.MarkOutliersAsBad)
            {
                await UpdateSensorOutlierCountsAsync(
                    result.ParticipatingSensorIds,
                    result.OutlierSensorIds,
                    previousMinuteStart,
                    cancellationToken);
            }

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            _logger.LogInformation(
                "Consensus calculated for {Minute}: " +
                "{Value:F2} °C using {SensorCount} sensors. " +
                "Outliers: {Outliers}",
                previousMinuteStart,
                result.ConsensusValue.Value,
                result.ParticipatingSensorIds.Count,
                result.OutlierSensorIds.Count == 0
                    ? "none"
                    : string.Join(
                        ", ",
                        result.OutlierSensorIds));
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }

    private async Task UpdateSensorOutlierCountsAsync(
        IReadOnlyCollection<string> participatingSensorIds,
        IReadOnlyCollection<string> outlierSensorIds,
        DateTime evaluatedMinute,
        CancellationToken cancellationToken)
    {
        var evaluatedSensorIds = participatingSensorIds
            .Concat(outlierSensorIds)
            .Distinct()
            .ToArray();

        if (evaluatedSensorIds.Length == 0)
        {
            return;
        }

        var sensors = await _dbContext.Sensors
            .Where(sensor =>
                evaluatedSensorIds.Contains(sensor.Id))
            .ToListAsync(cancellationToken);

        var outlierIdSet = outlierSensorIds.ToHashSet();

        foreach (var sensor in sensors)
        {
            var isOutlier = outlierIdSet.Contains(sensor.Id);

            if (!isOutlier)
            {
                // A valid value breaks the sequence of outlier periods
                sensor.ConsecutiveOutlierCount = 0;
                sensor.LastOutlierMinute = null;
                continue;
            }

            var expectedPreviousOutlierMinute =
                evaluatedMinute.AddMinutes(-1);

            // Increase the counter only when the previous outlier
            // occurred in the immediately preceding minute
            if (sensor.LastOutlierMinute ==
                expectedPreviousOutlierMinute)
            {
                sensor.ConsecutiveOutlierCount++;
            }
            else
            {
                sensor.ConsecutiveOutlierCount = 1;
            }

            sensor.LastOutlierMinute = evaluatedMinute;

            _logger.LogWarning(
                "Sensor {SensorId} was detected as an outlier. " +
                "Consecutive count: {Count}/{Threshold}.",
                sensor.Id,
                sensor.ConsecutiveOutlierCount,
                _options.OutlierStrikeThreshold);

            if (sensor.ConsecutiveOutlierCount <
                _options.OutlierStrikeThreshold)
            {
                continue;
            }

            sensor.DataQuality = DataQuality.Bad;

            _logger.LogWarning(
                "Sensor {SensorId} was marked as BAD after " +
                "{Count} consecutive outlier periods.",
                sensor.Id,
                sensor.ConsecutiveOutlierCount);
        }
    }

    private static DateTime TruncateToMinute(
        DateTime dateTime)
    {
        var utcDateTime = dateTime.Kind == DateTimeKind.Utc
            ? dateTime
            : dateTime.ToUniversalTime();

        return new DateTime(
            utcDateTime.Year,
            utcDateTime.Month,
            utcDateTime.Day,
            utcDateTime.Hour,
            utcDateTime.Minute,
            0,
            DateTimeKind.Utc);
    }
}