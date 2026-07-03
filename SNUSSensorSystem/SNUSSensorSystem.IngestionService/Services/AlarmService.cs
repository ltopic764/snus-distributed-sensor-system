using SNUSSensorSystem.Shared.DTOs;
using SNUSSensorSystem.Shared.Enums;
using SNUSSensorSystem.Shared.Models;

namespace SNUSSensorSystem.IngestionService.Services
{
    public class AlarmService : IAlarmService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AlarmService> _logger;

        private static readonly object ConsoleLock = new();
        
        public AlarmService(HttpClient httpClient, ILogger<AlarmService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task HandleAsync(SensorReading reading, CancellationToken cancellationToken = default)
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
                Timestamp = reading.Timestamp,
            };

            try
            {
                // .. match with what colleague defines this route
                // dummy for now
                var response = await _httpClient.PostAsJsonAsync("/api/notify", alarm, cancellationToken);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Alarm for sensor {SensorId} could not be sent to NotificationService", reading.SensorId);
            }
        }
                
        private void PrintColoredAlarm(SensorReading reading)
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
                var previous = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.WriteLine(
                    $"[ALARM P{(int)reading.AlarmPriority}] Sensor '{reading.SensorId}' " + $"=> value {reading.Value:F2} at {reading.Timestamp:yyyy-MM-dd HH:mm:ss}"
                    );
                Console.ForegroundColor = previous;
            }
        }

    }
}
