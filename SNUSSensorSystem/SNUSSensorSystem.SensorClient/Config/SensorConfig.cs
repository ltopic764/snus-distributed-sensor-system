using SNUSSensorSystem.Shared.Enums;

namespace SNUSSensorSystem.SensorClient.Config;

public sealed class SensorConfig
{
    public string SensorId { get; init; } = string.Empty;

    public double MinTemperature { get; init; }

    public double MaxTemperature { get; init; }

    public DataQuality DataQuality { get; init; } = DataQuality.Good;

    public double Priority1Low { get; init; }

    public double Priority1High { get; init; }

    public double Priority2Low { get; init; }

    public double Priority2High { get; init; }

    public double Priority3Low { get; init; }

    public double Priority3High { get; init; }

    public int MinSendIntervalSeconds { get; init; } = 1;

    public int MaxSendIntervalSeconds { get; init; } = 10;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(SensorId))
        {
            throw new InvalidOperationException("SensorId is required.");
        }

        if (MinTemperature >= MaxTemperature)
        {
            throw new InvalidOperationException(
                $"Invalid temperature range for {SensorId}.");
        }

        if (MinSendIntervalSeconds < 1 ||
            MaxSendIntervalSeconds < MinSendIntervalSeconds)
        {
            throw new InvalidOperationException(
                $"Invalid send interval for {SensorId}.");
        }

        if (!(Priority3Low <= Priority2Low &&
              Priority2Low <= Priority1Low &&
              Priority1Low < Priority1High &&
              Priority1High <= Priority2High &&
              Priority2High <= Priority3High))
        {
            throw new InvalidOperationException(
                $"Invalid alarm thresholds for {SensorId}.");
        }
    }
}