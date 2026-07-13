using SNUSSensorSystem.NotificationService.Models;
using SNUSSensorSystem.Shared.DTOs;

namespace SNUSSensorSystem.NotificationService.Services;

public interface INotificationBroadcastService
{
    Task<AlarmNotificationDto> BroadcastAlarmAsync(
        AlarmDto alarm,
        CancellationToken cancellationToken = default);
}
