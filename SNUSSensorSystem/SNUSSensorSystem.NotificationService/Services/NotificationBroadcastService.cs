using Microsoft.AspNetCore.SignalR;
using SNUSSensorSystem.NotificationService.Hubs;
using SNUSSensorSystem.NotificationService.Models;
using SNUSSensorSystem.Shared.DTOs;
using SNUSSensorSystem.Shared.Enums;

namespace SNUSSensorSystem.NotificationService.Services;

public sealed class NotificationBroadcastService : INotificationBroadcastService
{
    public const string AlarmClientMethod = "AlarmReceived";

    private readonly IHubContext<AlarmHub> _hubContext;
    private readonly ILogger<NotificationBroadcastService> _logger;

    public NotificationBroadcastService(
        IHubContext<AlarmHub> hubContext,
        ILogger<NotificationBroadcastService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task<AlarmNotificationDto> BroadcastAlarmAsync(
        AlarmDto alarm,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(alarm);

        var notification = new AlarmNotificationDto
        {
            SensorId = alarm.SensorId,
            Value = alarm.Value,
            Priority = alarm.Priority,
            PriorityName = alarm.Priority.ToString(),
            Color = GetColor(alarm.Priority),
            Timestamp = alarm.Timestamp.ToUniversalTime()
        };

        await _hubContext.Clients.All.SendAsync(
            AlarmClientMethod,
            notification,
            cancellationToken);

        _logger.LogInformation(
            "Alarm broadcast to SignalR clients. SensorId: {SensorId}, Value: {Value}, Priority: {Priority}, Color: {Color}, Timestamp: {Timestamp}",
            notification.SensorId,
            notification.Value,
            notification.Priority,
            notification.Color,
            notification.Timestamp);

        return notification;
    }

    private static string GetColor(AlarmPriority priority)
    {
        return priority switch
        {
            AlarmPriority.Low => "yellow",
            AlarmPriority.Medium => "orange",
            AlarmPriority.High => "red",
            _ => throw new ArgumentOutOfRangeException(
                nameof(priority),
                priority,
                "Only alarm priorities 1, 2 and 3 can be broadcast.")
        };
    }
}
