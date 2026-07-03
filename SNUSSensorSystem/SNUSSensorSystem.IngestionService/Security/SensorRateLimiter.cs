using System.Collections.Concurrent;

namespace SNUSSensorSystem.IngestionService.Security
{
    public class SensorRateLimiter : ISensorRateLimiter
    {
        private readonly int _maxMessagesPerWindow;
        private readonly TimeSpan _window;
        private readonly TimeSpan _blockDuration;

        private readonly ILogger<SensorRateLimiter> _logger;

        private readonly ConcurrentDictionary<string, SensorWindow> _windows = new();

        public SensorRateLimiter(ILogger<SensorRateLimiter> logger, int maxMessagesPerWindow = 10, int windowMilliseconds = 1000, int blockSeconds = 5)
        {
            _logger = logger;
            _maxMessagesPerWindow = maxMessagesPerWindow;
            _window = TimeSpan.FromMilliseconds(windowMilliseconds);
            _blockDuration = TimeSpan.FromSeconds(blockSeconds);
        }

        public bool IsAllowed(string sensorId)
        {
            if (string.IsNullOrWhiteSpace(sensorId))
            {
                return false;
            }

            var now = DateTime.UtcNow;
            var state = _windows.GetOrAdd(sensorId, _ => new SensorWindow());

            lock (state.Gate)
            {
                if (state.BlockedUntil.HasValue && state.BlockedUntil.Value > now)
                {
                    return false;
                }

                while (state.Timestamps.Count > 0 && now - state.Timestamps.Peek() > _window)
                    state.Timestamps.Dequeue();

                state.Timestamps.Enqueue(now);

                if (state.Timestamps.Count > _maxMessagesPerWindow)
                {
                    state.BlockedUntil = now + _blockDuration;
                    state.Timestamps.Clear();
                    _logger.LogWarning(
                        "Sensor {SensorId} temporarily blocked (>{Max} message/{Window}ms)", sensorId, _maxMessagesPerWindow, _window.TotalMilliseconds
                        );
                    return false;
                }
                return true;
            }
        }

        private sealed class SensorWindow
        {
            // lock
            public readonly object Gate = new();

            // timestamps of messages in current window
            public readonly Queue<DateTime> Timestamps = new();

            public DateTime? BlockedUntil;
        }
    }
}
