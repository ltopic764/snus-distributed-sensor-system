using SNUSSensorSystem.Shared.Models;

namespace SNUSSensorSystem.ConsensusService.Algorithms;

public interface IBftConsensusAlgorithm
{
    BftConsensusResult Calculate(
        IReadOnlyCollection<SensorReading> readings);
}