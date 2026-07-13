using Microsoft.AspNetCore.Mvc;
using SNUSSensorSystem.NotificationService.Services;
using SNUSSensorSystem.Shared.DTOs;
using SNUSSensorSystem.Shared.Enums;

namespace SNUSSensorSystem.NotificationService.Controllers;

[ApiController]
[Route("api/notify")]
public sealed class NotifyController : ControllerBase
{
    private readonly INotificationBroadcastService _broadcastService;
    private readonly ILogger<NotifyController> _logger;

    public NotifyController(
        INotificationBroadcastService broadcastService,
        ILogger<NotifyController> logger)
    {
        _broadcastService = broadcastService;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Notify(
        [FromBody] AlarmDto alarm,
        CancellationToken cancellationToken)
    {
        if (alarm is null)
        {
            return BadRequest(new { error = "Alarm payload is required." });
        }

        if (string.IsNullOrWhiteSpace(alarm.SensorId))
        {
            return BadRequest(new { error = "SensorId is required." });
        }

        if (!Enum.IsDefined(alarm.Priority) || alarm.Priority == AlarmPriority.None)
        {
            return BadRequest(new { error = "Priority must be Low (1), Medium (2) or High (3)." });
        }

        if (double.IsNaN(alarm.Value) || double.IsInfinity(alarm.Value))
        {
            return BadRequest(new { error = "Value must be a finite number." });
        }

        if (alarm.Timestamp == default)
        {
            return BadRequest(new { error = "Timestamp is required." });
        }

        try
        {
            var notification = await _broadcastService.BroadcastAlarmAsync(
                alarm,
                cancellationToken);

            return Accepted(new
            {
                status = "broadcast",
                alarm = notification
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation(
                "Alarm notification request was cancelled. SensorId: {SensorId}",
                alarm.SensorId);

            return StatusCode(499);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to broadcast alarm. SensorId: {SensorId}, Priority: {Priority}",
                alarm.SensorId,
                alarm.Priority);

            return Problem(
                title: "Alarm broadcast failed",
                detail: "The alarm could not be delivered to SignalR clients.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
