using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SNUSSensorSystem.IngestionService.Data;
using SNUSSensorSystem.Shared.Enums;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace SNUSSensorSystem.IngestionService.Controllers
{
    [ApiController]
    [Route("api/reports")]
    public class ReportController : ControllerBase
    {
        private readonly SensorDbContext _db;
        private readonly ILogger<ReportController> _logger;

        private const int DefaultLimit = 200;
        private const int MaxLimit = 1000;

        public ReportController(SensorDbContext db, ILogger<ReportController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // raw history readings
        [HttpGet("readings")]
        public async Task<IActionResult> GetReadings(
            [FromQuery] string? sensorId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int? limit,
            CancellationToken cancellationToken)
        {
            // validate date if given
            if (from.HasValue && to.HasValue && from > to)
            {
                return BadRequest("'from' cannot be after 'to'");
            }

            var take = NormalizeLimit(limit);

            // building the query
            var query = _db.SensorReadings
                .AsNoTracking()
                .Where(reading => !reading.IsConsensus); // raw readings

            if (!string.IsNullOrWhiteSpace(sensorId))
            {
                query = query.Where(reading => reading.SensorId == sensorId);
            }

            if (from.HasValue)
            {
                var fromUtc = ToUtc(from.Value);
                query = query.Where(reading => reading.Timestamp >= fromUtc);
            }

            if (to.HasValue)
            {
                var toUtc = ToUtc(to.Value);
                query = query.Where(reading => reading.Timestamp <= toUtc);
            }

            var readings = await query
                .OrderByDescending(reading => reading.Timestamp)
                .Take(take)
                .Select(reading => new
                {
                    reading.Id,
                    reading.SensorId,
                    reading.Value,
                    reading.Timestamp,
                    AlarmPriority = (int)reading.AlarmPriority,     // 0/1/2/3
                    DataQuality = reading.DataQuality.ToString(),   // "Good"/"Bad"/...
                    reading.ReceivedAt
                })
                .ToListAsync(cancellationToken);

            return Ok(new { count = readings.Count, items = readings });
        }

        // picking up alarms
        [HttpGet("alarms")]
        public async Task<IActionResult> GetAlarms(
            [FromQuery] string? sensorId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int? limit,
            CancellationToken cancellationToken)
        {
            // validate date if given
            if (from.HasValue && to.HasValue && from > to)
            {
                return BadRequest("'from' cannot be after 'to'");
            }

            var take = NormalizeLimit(limit);

            // building the query
            var query = _db.SensorReadings
                .AsNoTracking()
                .Where(reading => reading.AlarmPriority != AlarmPriority.None); // raw readings

            if (!string.IsNullOrWhiteSpace(sensorId))
                query = query.Where(reading => reading.SensorId == sensorId);

            if (from.HasValue)
            {
                var fromUtc = ToUtc(from.Value);
                query = query.Where(reading => reading.Timestamp >= fromUtc);
            }

            if (to.HasValue)
            {
                var toUtc = ToUtc(to.Value);
                query = query.Where(reading => reading.Timestamp <= toUtc);
            }

            var alarms = await query
                .OrderByDescending(reading => reading.Timestamp)
                .Take(take)
                .Select(reading => new
                {
                    reading.Id,
                    reading.SensorId,
                    reading.Value,
                    reading.Timestamp,
                    Priority = (int)reading.AlarmPriority
                })
                .ToListAsync(cancellationToken);

            return Ok(new { count = alarms.Count, items = alarms });
        }

        // consensus values
        [HttpGet("consensus")]
        public async Task<IActionResult> GetConsensus(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int? limit,
            CancellationToken cancellationToken)
        {
            // validate date if given
            if (from.HasValue && to.HasValue && from > to)
            {
                return BadRequest("'from' cannot be after 'to'");
            }

            var take = NormalizeLimit(limit);

            var query = _db.ConsensusValues.AsNoTracking();

            if (from.HasValue)
            {
                var fromUtc = ToUtc(from.Value);
                query = query.Where(value => value.Timestamp >= fromUtc);
            }

            if (to.HasValue)
            {
                var toUtc = ToUtc(to.Value);
                query = query.Where(value => value.Timestamp <= toUtc);
            }

            var consensus = await query
                .OrderByDescending(value => value.Timestamp)
                .Take(take)
                .Select(value => new
                {
                    value.Id,
                    value.Value,
                    value.Timestamp,
                    value.CalculatedAt,
                    value.ParticipatingSensorCount
                })
                .ToListAsync(cancellationToken);

            return Ok(new { count = consensus.Count, items = consensus });
        }

        // helpers
        private static int NormalizeLimit(int? requested)
        {
            if (requested is null || requested <= 0)
                return DefaultLimit;

            return Math.Min(requested.Value, MaxLimit);
        }

        private static DateTime ToUtc(DateTime value) =>
            value.Kind == DateTimeKind.Utc
                ? value
                : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }
}
