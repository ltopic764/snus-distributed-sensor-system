using SNUSSensorSystem.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SNUSSensorSystem.Shared.DTOs
{
    // for reports
    public class SensorStatusDto
    {
        public string SensorId { get; set; } = string.Empty;

        // is active
        public bool isActive { get; set; }

        // time of the last received mess
        public DateTime? LastMessageReceivedAt { get; set; }

        public DataQuality DataQuality { get; set; }
    }
}
