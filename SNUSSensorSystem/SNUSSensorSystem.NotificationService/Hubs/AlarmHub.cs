using Microsoft.AspNetCore.SignalR;

namespace SNUSSensorSystem.NotificationService.Hubs;

public sealed class AlarmHub : Hub
{
    private readonly ILogger<AlarmHub> _logger;

    public AlarmHub(ILogger<AlarmHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation(
            "SignalR client connected. ConnectionId: {ConnectionId}",
            Context.ConnectionId);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception is null)
        {
            _logger.LogInformation(
                "SignalR client disconnected. ConnectionId: {ConnectionId}",
                Context.ConnectionId);
        }
        else
        {
            _logger.LogWarning(
                exception,
                "SignalR client disconnected with an error. ConnectionId: {ConnectionId}",
                Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
