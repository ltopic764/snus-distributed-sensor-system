namespace SNUSSensorSystem.ConsensusService.Services;

public interface IConsensusCalculatorService
{
    Task CalculateForPreviousMinuteAsync(
        DateTime currentUtcTime,
        CancellationToken cancellationToken);
}