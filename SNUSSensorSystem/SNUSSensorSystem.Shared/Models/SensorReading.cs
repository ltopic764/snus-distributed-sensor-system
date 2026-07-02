using SNUSSensorSystem.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SNUSSensorSystem.Shared.Models
{
    // temperature reading that the sensor sent and is then saved in db
    public class SensorReading
    {
        public long Id { get; set; }

        // sensorId that sent the reading
        public string SensorId { get; set; } = string.Empty;

        public double Value { get; set; }

        // when the reading was done
        public DateTime Timestamp { get; set; }

        public AlarmPriority AlarmPriority { get; set; } = AlarmPriority.None;

        public DataQuality DataQuality { get; set; } = DataQuality.Good;

        public bool IsConsensus { get; set; } = false; // consensus is saved in table ConsensusValues!!! here always false, we calculate it in different spot

        // when the server received the message
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    }
}
