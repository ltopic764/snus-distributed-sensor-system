namespace SNUSSensorSystem.ConsensusService.Algorithms;

public class BftConsensusResult
{
    public bool Success { get; init; }

    public double? ConsensusValue { get; init; }

    public double Median { get; init; }

    public double MedianAbsoluteDeviation { get; init; }

    public IReadOnlyCollection<string> ParticipatingSensorIds
    { get; init; } = Array.Empty<string>();

    public IReadOnlyCollection<string> OutlierSensorIds
    { get; init; } = Array.Empty<string>();

    public string? FailureReason { get; init; }
}