using SNUSSensorSystem.Shared.Enums;

namespace SNUSSensorSystem.NotificationService.Models;

public sealed class AlarmNotificationDto
{
    public string SensorId { get; init; } = string.Empty;

    public double Value { get; init; }

    public AlarmPriority Priority { get; init; }

    public string PriorityName { get; init; } = string.Empty;

    public string Color { get; init; } = string.Empty;

    public DateTime Timestamp { get; init; }
}
