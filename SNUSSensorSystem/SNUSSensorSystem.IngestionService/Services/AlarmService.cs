using System.Net.Http.Json;
using SNUSSensorSystem.Shared.DTOs;
using SNUSSensorSystem.Shared.Enums;
using SNUSSensorSystem.Shared.Models;

namespace SNUSSensorSystem.IngestionService.Services;

public sealed class AlarmService : IAlarmService
{
    private const string NotifyEndpoint = "api/notify";

    private static readonly object ConsoleLock = new();

    private readonly HttpClient _httpClient;
    private readonly ILogger<AlarmService> _logger;

    public AlarmService(
        HttpClient httpClient,
        ILogger<AlarmService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task HandleAsync(
        SensorReading reading,
        CancellationToken cancellationToken = default)
    {
        // no alarm, do nothing
        if (reading.AlarmPriority == AlarmPriority.None)
        {
            return;
        }

        // console log
        PrintColoredAlarm(reading);

        // notify NotificationService
        var alarm = new AlarmDto
        {
            SensorId = reading.SensorId,
            Value = reading.Value,
            Priority = reading.AlarmPriority,
            Timestamp = reading.Timestamp
        };

        try
        {
            using var response =
                await _httpClient.PostAsJsonAsync(
                    NotifyEndpoint,
                    alarm,
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var responseBody =
                    await response.Content.ReadAsStringAsync(
                        cancellationToken);

                _logger.LogWarning(
                    "NotificationService rejected alarm. " +
                    "SensorId: {SensorId}, " +
                    "StatusCode: {StatusCode}, " +
                    "Response: {Response}",
                    reading.SensorId,
                    (int)response.StatusCode,
                    responseBody);

                return;
            }

            _logger.LogInformation(
                "Alarm sent to NotificationService. " +
                "SensorId: {SensorId}, Priority: {Priority}",
                reading.SensorId,
                reading.AlarmPriority);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation(
                "Sending alarm was cancelled. SensorId: {SensorId}",
                reading.SensorId);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(
                ex,
                "NotificationService request timed out. " +
                "SensorId: {SensorId}",
                reading.SensorId);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(
                ex,
                "NotificationService is unavailable. " +
                "SensorId: {SensorId}",
                reading.SensorId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error while sending alarm to " +
                "NotificationService. SensorId: {SensorId}",
                reading.SensorId);
        }
    }

    private static void PrintColoredAlarm(
        SensorReading reading)
    {
        var color = reading.AlarmPriority switch
        {
            AlarmPriority.Low => ConsoleColor.Yellow,
            AlarmPriority.Medium => ConsoleColor.DarkYellow,
            AlarmPriority.High => ConsoleColor.Red,
            _ => ConsoleColor.Gray
        };

        lock (ConsoleLock)
        {
            var previousColor = Console.ForegroundColor;

            try
            {
                Console.ForegroundColor = color;

                Console.WriteLine(
                    "[ALARM P{0}] Sensor '{1}' => " +
                    "value {2:F2} at {3:yyyy-MM-dd HH:mm:ss}",
                    (int)reading.AlarmPriority,
                    reading.SensorId,
                    reading.Value,
                    reading.Timestamp);
            }
            finally
            {
                Console.ForegroundColor = previousColor;
            }
        }
    }
}