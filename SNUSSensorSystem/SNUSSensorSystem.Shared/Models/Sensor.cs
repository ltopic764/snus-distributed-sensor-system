using SNUSSensorSystem.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SNUSSensorSystem.Shared.Models
{
    public class Sensor
    {
        public string Id { get; set; } = string.Empty;

        // lower bound for generating temperature
        public double MinRange { get; set; }

        // upper bound
        public double MaxRange { get; set; }

        // current data quality, initially Good
        // ConsensusService changes it to Bad if sensor malicious
        public DataQuality DataQuality { get; set; } = DataQuality.Good;

        // time interval of the last received message
        // this is used to check if the sensor is alive
        public DateTime? LastMessageReceivedAt { get; set; }

        // is sensor currently active, cached flag for faster checking
        public bool IsActive { get; set; }

        // sensors public key, used for verifying digital signature of the message
        // using this key it is checked whether the message was sent by the snesor that says he sent it
        public string? PublicKey { get; set; }

        // last messageId that was received from this sensor
        // used for protection of replay attacks
        public long LastMessageId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
