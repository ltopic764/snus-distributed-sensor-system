using System.Net.Http.Json;
using SNUSSensorSystem.SensorClient.Config;
using SNUSSensorSystem.SensorClient.Security;
using SNUSSensorSystem.Shared.DTOs;
using SNUSSensorSystem.Shared.Enums;

namespace SNUSSensorSystem.SensorClient.Sensor;

public sealed class SensorSimulator
{
    private static readonly object ConsoleLock = new();

    private readonly SensorConfig _config;
    private readonly HttpClient _httpClient;
    private readonly ClientCryptoService _crypto;
    private readonly Random _random;
    private readonly object _stateLock = new();

    private DateTime? _blockedUntilUtc;
    private bool _isActive;

    public SensorSimulator(
        SensorConfig config,
        HttpClient httpClient,
        ClientCryptoService crypto)
    {
        config.Validate();

        _config = config;
        _httpClient = httpClient;
        _crypto = crypto;

        _random = new Random(
            StringComparer.Ordinal.GetHashCode(
                config.SensorId));
    }

    public string SensorId => _config.SensorId;

    public DateTime LastSuccessfulTransmissionUtc
    {
        get;
        private set;
    } = DateTime.UtcNow;

    public bool IsActive
    {
        get
        {
            lock (_stateLock)
            {
                return _isActive;
            }
        }
    }

    public bool IsBlocked
    {
        get
        {
            lock (_stateLock)
            {
                if (_blockedUntilUtc is null)
                {
                    return false;
                }

                if (_blockedUntilUtc > DateTime.UtcNow)
                {
                    return true;
                }

                _blockedUntilUtc = null;

                return false;
            }
        }
    }

    public void Activate()
    {
        lock (_stateLock)
        {
            if (_blockedUntilUtc > DateTime.UtcNow)
            {
                return;
            }

            _isActive = true;

            LastSuccessfulTransmissionUtc =
                DateTime.UtcNow;
        }

        WriteLine(
            ConsoleColor.Green,
            $"[{SensorId}] ACTIVE");
    }

    public void Deactivate(string reason)
    {
        lock (_stateLock)
        {
            _isActive = false;
        }

        WriteLine(
            ConsoleColor.DarkYellow,
            $"[{SensorId}] STANDBY ({reason})");
    }

    public void Block(TimeSpan duration)
    {
        lock (_stateLock)
        {
            _blockedUntilUtc =
                DateTime.UtcNow.Add(duration);

            _isActive = false;
        }

        WriteLine(
            ConsoleColor.Magenta,
            $"[{SensorId}] BLOCKED for " +
            $"{duration.TotalSeconds:0} seconds");
    }

    public async Task RunAsync(
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (!IsActive || IsBlocked)
            {
                await Task.Delay(
                    500,
                    cancellationToken);

                continue;
            }

            await SendReadingAsync(
                cancellationToken);

            var delaySeconds = _random.Next(
                _config.MinSendIntervalSeconds,
                _config.MaxSendIntervalSeconds + 1);

            await Task.Delay(
                TimeSpan.FromSeconds(delaySeconds),
                cancellationToken);
        }
    }

    private async Task SendReadingAsync(
        CancellationToken cancellationToken)
    {
        var randomPart = _random.NextDouble() *
                         (_config.MaxTemperature -
                          _config.MinTemperature);

        var temperature =
            _config.MinTemperature + randomPart;

        var alarmPriority =
            DetectAlarm(temperature);

        var reading = new SensorReadingDto
        {
            SensorId = SensorId,
            Value = Math.Round(temperature, 2),
            Timestamp = DateTime.UtcNow,
            AlarmPriority = alarmPriority,
            DataQuality = _config.DataQuality
        };

        WriteReading(reading);

        try
        {
            var encryptedEnvelope =
                _crypto.Protect(reading);

            using var response =
                await _httpClient.PostAsJsonAsync(
                    "api/ingest",
                    encryptedEnvelope,
                    cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                LastSuccessfulTransmissionUtc =
                    DateTime.UtcNow;

                return;
            }

            var responseBody =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            WriteLine(
                ConsoleColor.Red,
                $"[{SensorId}] Server rejected message: " +
                $"{(int)response.StatusCode} " +
                $"{responseBody}");
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            WriteLine(
                ConsoleColor.Red,
                $"[{SensorId}] Send failed: " +
                $"{exception.Message}");
        }
    }

    private AlarmPriority DetectAlarm(double value)
    {
        if (value <= _config.Priority3Low ||
            value >= _config.Priority3High)
        {
            return AlarmPriority.High;
        }

        if (value <= _config.Priority2Low ||
            value >= _config.Priority2High)
        {
            return AlarmPriority.Medium;
        }

        if (value <= _config.Priority1Low ||
            value >= _config.Priority1High)
        {
            return AlarmPriority.Low;
        }

        return AlarmPriority.None;
    }

    private void WriteReading(
        SensorReadingDto reading)
    {
        var color = reading.AlarmPriority switch
        {
            AlarmPriority.Low =>
                ConsoleColor.Yellow,

            AlarmPriority.Medium =>
                ConsoleColor.DarkYellow,

            AlarmPriority.High =>
                ConsoleColor.Red,

            _ =>
                ConsoleColor.Gray
        };

        var alarmText =
            reading.AlarmPriority ==
            AlarmPriority.None
                ? "OK"
                : $"ALARM P" +
                  $"{(int)reading.AlarmPriority}";

        WriteLine(
            color,
            $"[{SensorId}] " +
            $"{reading.Value,6:F2} °C | " +
            $"{alarmText} | " +
            $"{reading.Timestamp:HH:mm:ss}");
    }

    private static void WriteLine(
        ConsoleColor color,
        string text)
    {
        lock (ConsoleLock)
        {
            var previousColor =
                Console.ForegroundColor;

            Console.ForegroundColor = color;

            Console.WriteLine(text);

            Console.ForegroundColor =
                previousColor;
        }
    }
}