using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SNUSSensorSystem.IngestionService.Data;
using SNUSSensorSystem.IngestionService.Security;
using SNUSSensorSystem.IngestionService.Services;
using SNUSSensorSystem.Shared.DTOs;
using SNUSSensorSystem.Shared.Models;

namespace SNUSSensorSystem.IngestionService.Controllers
{
    // sensors send reading here, in SecureEnvelopeDto

    // message handling
    [ApiController]
    [Route("api/ingest")]
    public class IngestController : ControllerBase
    {
        private readonly SensorDbContext _db;
        private readonly IMessageSecurityService _security;
        private readonly ISensorRateLimiter _rateLimiter;
        private readonly IAlarmService _alarmService;
        private readonly ILogger<IngestController> _logger;

        public IngestController(
            SensorDbContext db,
            IMessageSecurityService security,
            ISensorRateLimiter rateLimiter,
            IAlarmService alarmService,
            ILogger<IngestController> logger
            )
        {
            _db = db;
            _security = security;
            _rateLimiter = rateLimiter;
            _alarmService = alarmService;
            _logger = logger;
        }

        // receiving reading
        [HttpPost]
        public async Task<IActionResult> Ingest([FromBody] SecureEnvelopeDto envelope, CancellationToken cancellationToken)
        {
            if (envelope is null || string.IsNullOrWhiteSpace(envelope.SensorId))
            {
                return BadRequest("Invalid or empty message envelope");
            }

            // Dos
            if (!_rateLimiter.IsAllowed(envelope.SensorId))
            {
                _logger.LogWarning("Message denied: sensor {SensorId} rate-limited", envelope.SensorId);
                return StatusCode(StatusCodes.Status429TooManyRequests, "Sensor temporarily blocked because of too many messages");
            }

            // verify signature, anti-replay, decrypt
            var check = await _security.VerifyAndDecryptAsync(envelope, cancellationToken);

            if (!check.IsValid || check.Payload is null)
            {
                _logger.LogWarning("Sensor message denied {SensorId}:  {Error}", envelope.SensorId, check.Error);
                return Unauthorized(check.Error);
            }

            var dto = check.Payload;

            // map dto
            var reading = new SensorReading
            {
                SensorId = dto.SensorId,
                Value = dto.Value,
                Timestamp = dto.Timestamp.ToUniversalTime(),
                AlarmPriority = dto.AlarmPriority,
                DataQuality = dto.DataQuality,
                IsConsensus = false, // raw read is never consensus
                ReceivedAt = DateTime.UtcNow
            };
            _db.SensorReadings.Add(reading);

            var sensor = await _db.Sensors.FindAsync(new object?[] { dto.SensorId }, cancellationToken);

            if (sensor is not null)
            {
                sensor.LastMessageReceivedAt = DateTime.UtcNow;
                sensor.IsActive = true;
            }

            await _db.SaveChangesAsync(cancellationToken);

            await _alarmService.HandleAsync(reading, cancellationToken);

            return Ok(new { reading.Id, status = "received" });
        }

        // overall sensor statuses
        [HttpGet("sensors")]
        public async Task<ActionResult<IEnumerable<SensorStatusDto>>> GetSensors(CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var inactivityThreshold = TimeSpan.FromSeconds(10);

            var sensors = await _db.Sensors
                .AsNoTracking()
                .Select(s => new SensorStatusDto
                {
                    SensorId = s.Id,
                    LastMessageReceivedAt = s.LastMessageReceivedAt,
                    DataQuality = s.DataQuality,
                    IsActive = s.LastMessageReceivedAt != null && now - s.LastMessageReceivedAt.Value <= inactivityThreshold
                })
                .ToListAsync(cancellationToken);

            return Ok(sensors);
        }

    }
}
