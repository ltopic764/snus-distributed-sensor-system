using SNUSSensorSystem.ConsensusService.Services;

namespace SNUSSensorSystem.ConsensusService.Workers;

public class ConsensusWorker
{
    private readonly IConsensusCalculatorService
        _consensusCalculatorService;

    private readonly ILogger<ConsensusWorker> _logger;

    public ConsensusWorker(
        IConsensusCalculatorService consensusCalculatorService,
        ILogger<ConsensusWorker> logger)
    {
        _consensusCalculatorService =
            consensusCalculatorService;

        _logger = logger;
    }

    public async Task ExecuteOnceAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await _consensusCalculatorService
                .CalculateForPreviousMinuteAsync(
                    DateTime.UtcNow,
                    cancellationToken);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            // Normal application shutdown
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "An error occurred while calculating consensus.");
        }
    }
}