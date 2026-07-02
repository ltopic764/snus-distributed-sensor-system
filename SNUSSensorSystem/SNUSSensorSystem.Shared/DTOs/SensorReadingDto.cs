using SNUSSensorSystem.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SNUSSensorSystem.Shared.DTOs
{
    // this is not sent to server directly
    // it is serialized into a JSON, the encrypted, and given to SecureEnvelopeDto
    public class SensorReadingDto
    {
        public string SensorId { get; set; } = string.Empty;

        // read temp
        public double Value { get; set; }

        // time of reading
        public DateTime Timestamp { get; set; }

        public AlarmPriority AlarmPriority { get; set; } = AlarmPriority.None;

        public DataQuality DataQuality { get; set; } = DataQuality.Good;
    }
}
