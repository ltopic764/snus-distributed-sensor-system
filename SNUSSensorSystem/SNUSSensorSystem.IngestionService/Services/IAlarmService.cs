using SNUSSensorSystem.Shared.Models;

namespace SNUSSensorSystem.IngestionService.Services
{
    public interface IAlarmService
    {
        // handling temp reading keeping track of the alarm value
        Task HandleAsync(SensorReading reading, CancellationToken cancellationToken = default);
    }
}
