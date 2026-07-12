namespace SNUSSensorSystem.SensorClient.Sensor;

public sealed class SensorFleetManager
{
    private readonly IReadOnlyList<SensorSimulator> _sensors;
    private readonly int _requiredActiveCount;
    private readonly TimeSpan _inactivityThreshold;
    private readonly object _sync = new();

    public SensorFleetManager(
        IReadOnlyList<SensorSimulator> sensors,
        int requiredActiveCount = 5,
        TimeSpan? inactivityThreshold = null)
    {
        if (sensors.Count < requiredActiveCount)
        {
            throw new ArgumentException(
                "There must be at least as many sensors " +
                "as required active sensors.",
                nameof(sensors));
        }

        _sensors = sensors;
        _requiredActiveCount = requiredActiveCount;

        _inactivityThreshold =
            inactivityThreshold ??
            TimeSpan.FromSeconds(10);
    }

    public async Task RunAsync(
        CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            foreach (var sensor in
                     _sensors.Take(_requiredActiveCount))
            {
                sensor.Activate();
            }
        }

        var sensorTasks = _sensors
            .Select(sensor =>
                sensor.RunAsync(cancellationToken))
            .ToArray();

        var monitorTask =
            MonitorAsync(cancellationToken);

        var commandTask =
            ReadCommandsAsync(cancellationToken);

        await Task.WhenAll(
            sensorTasks
                .Append(monitorTask)
                .Append(commandTask));
    }

    public bool BlockSensor(
        string sensorId,
        TimeSpan duration)
    {
        lock (_sync)
        {
            var sensor = _sensors.FirstOrDefault(
                currentSensor =>
                    currentSensor.SensorId.Equals(
                        sensorId,
                        StringComparison.OrdinalIgnoreCase));

            if (sensor is null)
            {
                return false;
            }

            sensor.Block(duration);

            EnsureExactlyFiveActive();

            return true;
        }
    }

    private async Task MonitorAsync(
        CancellationToken cancellationToken)
    {
        using var timer =
            new PeriodicTimer(
                TimeSpan.FromSeconds(1));

        while (await timer.WaitForNextTickAsync(
                   cancellationToken))
        {
            lock (_sync)
            {
                foreach (var sensor in
                         _sensors
                             .Where(sensor =>
                                 sensor.IsActive)
                             .ToArray())
                {
                    if (sensor.IsBlocked)
                    {
                        sensor.Deactivate("blocked");
                    }
                    else if (
                        DateTime.UtcNow -
                        sensor.LastSuccessfulTransmissionUtc >
                        _inactivityThreshold)
                    {
                        sensor.Deactivate(
                            "no successful message for " +
                            $"{_inactivityThreshold.TotalSeconds:0}s");
                    }
                }

                EnsureExactlyFiveActive();
            }
        }
    }

    private void EnsureExactlyFiveActive()
    {
        var activeCount = _sensors.Count(
            sensor =>
                sensor.IsActive &&
                !sensor.IsBlocked);

        if (activeCount > _requiredActiveCount)
        {
            foreach (var sensor in
                     _sensors
                         .Where(sensor =>
                             sensor.IsActive)
                         .Skip(_requiredActiveCount))
            {
                sensor.Deactivate(
                    "more than five active sensors");
            }

            activeCount = _requiredActiveCount;
        }

        foreach (var reserveSensor in
                 _sensors.Where(sensor =>
                     !sensor.IsActive &&
                     !sensor.IsBlocked))
        {
            if (activeCount >= _requiredActiveCount)
            {
                break;
            }

            reserveSensor.Activate();

            activeCount++;
        }

        if (activeCount < _requiredActiveCount)
        {
            Console.WriteLine(
                $"WARNING: only " +
                $"{activeCount}/" +
                $"{_requiredActiveCount} " +
                "sensors can currently be active.");
        }
    }

    private async Task ReadCommandsAsync(
        CancellationToken cancellationToken)
    {
        Console.WriteLine(
            "Commands: block <sensor-id>, " +
            "status, help, quit");

        while (!cancellationToken.IsCancellationRequested)
        {
            string? line;

            try
            {
                line = await Console.In.ReadLineAsync(
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (line is null)
            {
                await Task.Delay(
                    Timeout.Infinite,
                    cancellationToken);

                return;
            }

            var parts = line.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);

            if (parts.Length == 0)
            {
                continue;
            }

            switch (parts[0].ToLowerInvariant())
            {
                case "block" when parts.Length == 2:
                    {
                        var successfullyBlocked =
                            BlockSensor(
                                parts[1],
                                TimeSpan.FromSeconds(30));

                        Console.WriteLine(
                            successfullyBlocked
                                ? $"Sensor {parts[1]} blocked."
                                : $"Unknown sensor: {parts[1]}");

                        break;
                    }

                case "status":
                    {
                        foreach (var sensor in _sensors)
                        {
                            Console.WriteLine(
                                $"{sensor.SensorId}: " +
                                $"active={sensor.IsActive}, " +
                                $"blocked={sensor.IsBlocked}, " +
                                $"lastSuccess=" +
                                $"{sensor.LastSuccessfulTransmissionUtc:O}");
                        }

                        break;
                    }

                case "help":
                    {
                        Console.WriteLine(
                            "block sensor-01 | status | quit");

                        break;
                    }

                case "quit":
                    {
                        Environment.Exit(0);

                        break;
                    }

                default:
                    {
                        Console.WriteLine(
                            "Unknown command. Type 'help'.");

                        break;
                    }
            }
        }
    }
}