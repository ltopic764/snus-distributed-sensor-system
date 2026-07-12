namespace SNUSSensorSystem.ConsensusService;

public class ConsensusOptions
{
    public const string SectionName = "Consensus";

    // How often the worker attempts to process the previous minute
    public int IntervalSeconds { get; set; } = 60;

    // Minimum number of reliable sensors required for consensus
    public int MinimumSensorCount { get; set; } = 2;

    // Multiplier for MAD outlier detection
    public double MadMultiplier { get; set; } = 3.5;

    // Used when MAD is zero
    public double MinimumAllowedDeviation { get; set; } = 2.0;

    // Whether detected outliers should be tracked
    // and eventually marked as BAD
    public bool MarkOutliersAsBad { get; set; } = true;

    // Number of consecutive outlier periods required
    // before the sensor is marked as BAD
    public int OutlierStrikeThreshold { get; set; } = 3;
}