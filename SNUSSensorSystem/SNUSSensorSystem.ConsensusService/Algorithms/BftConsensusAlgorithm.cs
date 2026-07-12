using Microsoft.Extensions.Options;
using SNUSSensorSystem.Shared.Enums;
using SNUSSensorSystem.Shared.Models;

namespace SNUSSensorSystem.ConsensusService.Algorithms;

public class BftConsensusAlgorithm : IBftConsensusAlgorithm
{
    private const double MadScaleFactor = 1.4826;

    private readonly ConsensusOptions _options;

    public BftConsensusAlgorithm(
        IOptions<ConsensusOptions> options)
    {
        _options = options.Value;
    }

    public BftConsensusResult Calculate(
        IReadOnlyCollection<SensorReading> readings)
    {
        ArgumentNullException.ThrowIfNull(readings);

        /*
         * First, we take only the quality measurements.
         *
         * Then, for each sensor, we calculate the average value.
         * This way, a sensor that sent more messages does not get a higher
         * weight than a sensor that sent one message.
         */
        var valuesBySensor = readings
            .Where(reading =>
                reading.DataQuality == DataQuality.Good &&
                !reading.IsConsensus)
            .GroupBy(reading => reading.SensorId)
            .Select(group => new SensorAggregate(
                group.Key,
                group.Average(reading => reading.Value)))
            .OrderBy(item => item.SensorId)
            .ToList();

        if (valuesBySensor.Count < _options.MinimumSensorCount)
        {
            return new BftConsensusResult
            {
                Success = false,
                FailureReason =
                    $"Not enough GOOD sensors. Found " +
                    $"{valuesBySensor.Count}, but at least " +
                    $"{_options.MinimumSensorCount} are required."
            };
        }

        var sensorValues = valuesBySensor
            .Select(item => item.Value)
            .OrderBy(value => value)
            .ToList();

        var median = CalculateMedian(sensorValues);

        /*
         * MAD = median absolute deviation.
         *
         * For each value, we calculate the distance from the median,
         * and then we calculate the median of those distances.
         *
         * MAD is more resistant to malicious values than the mean
         * and standard deviation.
         */
        var absoluteDeviations = sensorValues
            .Select(value => Math.Abs(value - median))
            .OrderBy(value => value)
            .ToList();

        var mad = CalculateMedian(absoluteDeviations);

        var allowedDeviation = mad > 0
            ? Math.Max(
                _options.MinimumAllowedDeviation,
                _options.MadMultiplier * MadScaleFactor * mad)
            : _options.MinimumAllowedDeviation;

        var participatingSensors = valuesBySensor
            .Where(item =>
                Math.Abs(item.Value - median) <= allowedDeviation)
            .ToList();

        var outlierSensors = valuesBySensor
            .Where(item =>
                Math.Abs(item.Value - median) > allowedDeviation)
            .ToList();

        if (participatingSensors.Count <
            _options.MinimumSensorCount)
        {
            return new BftConsensusResult
            {
                Success = false,
                Median = median,
                MedianAbsoluteDeviation = mad,

                ParticipatingSensorIds = participatingSensors
                    .Select(item => item.SensorId)
                    .ToArray(),

                OutlierSensorIds = outlierSensors
                    .Select(item => item.SensorId)
                    .ToArray(),

                FailureReason =
                    "Not enough sensors remained after " +
                    "outlier filtering."
            };
        }

        /*
         * After discarding outliers, the final consensus
         * is calculated as the average of the reliable sensors.
         */
        var consensusValue = participatingSensors
            .Average(item => item.Value);

        return new BftConsensusResult
        {
            Success = true,
            ConsensusValue = consensusValue,
            Median = median,
            MedianAbsoluteDeviation = mad,

            ParticipatingSensorIds = participatingSensors
                .Select(item => item.SensorId)
                .ToArray(),

            OutlierSensorIds = outlierSensors
                .Select(item => item.SensorId)
                .ToArray()
        };
    }

    private static double CalculateMedian(
        IReadOnlyList<double> sortedValues)
    {
        if (sortedValues.Count == 0)
        {
            throw new ArgumentException(
                "At least one value is required.",
                nameof(sortedValues));
        }

        var middleIndex = sortedValues.Count / 2;

        if (sortedValues.Count % 2 == 1)
        {
            return sortedValues[middleIndex];
        }

        return (
            sortedValues[middleIndex - 1] +
            sortedValues[middleIndex]
        ) / 2.0;
    }

    private sealed record SensorAggregate(
        string SensorId,
        double Value);
}