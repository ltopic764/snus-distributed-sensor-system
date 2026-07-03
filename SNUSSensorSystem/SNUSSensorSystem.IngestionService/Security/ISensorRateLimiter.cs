namespace SNUSSensorSystem.IngestionService.Security
{
    public interface ISensorRateLimiter
    {
        // returns false if a sensor is temporarily blocked because of the rate limiting when sending messages
        bool IsAllowed(string sensorId);
    }
}
