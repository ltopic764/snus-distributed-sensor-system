using Microsoft.Extensions.Options;
using SNUSSensorSystem.ConsensusService.Workers;

namespace SNUSSensorSystem.ConsensusService;

public class Worker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConsensusOptions _options;
    private readonly ILogger<Worker> _logger;

    public Worker(
        IServiceScopeFactory scopeFactory,
        IOptions<ConsensusOptions> options,
        ILogger<Worker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Consensus worker started. Interval: {Interval} seconds.",
            _options.IntervalSeconds);

        /*
         * Waiting until the next minute starts.
         * This way, the worker processes the minute that has just ended.
         * 
         */
        await WaitUntilNextMinuteAsync(stoppingToken);

        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(
                _options.IntervalSeconds));

        do
        {
            await RunConsensusCycleAsync(stoppingToken);
        }
        while (
            await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunConsensusCycleAsync(
        CancellationToken cancellationToken)
    {
        /*
         * Hosted Worker is singleton, while DbContext is scoped.
         * Therefore, for each cycle, we create a new DI scope.
         */
        await using var scope =
            _scopeFactory.CreateAsyncScope();

        var consensusWorker =
            scope.ServiceProvider
                .GetRequiredService<ConsensusWorker>();

        await consensusWorker.ExecuteOnceAsync(
            cancellationToken);
    }

    private static async Task WaitUntilNextMinuteAsync(
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var nextMinute = new DateTime(
            now.Year,
            now.Month,
            now.Day,
            now.Hour,
            now.Minute,
            0,
            DateTimeKind.Utc)
            .AddMinutes(1);

        var delay = nextMinute - now;

        await Task.Delay(delay, cancellationToken);
    }

    public override Task StopAsync(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Consensus worker is stopping.");

        return base.StopAsync(cancellationToken);
    }
}