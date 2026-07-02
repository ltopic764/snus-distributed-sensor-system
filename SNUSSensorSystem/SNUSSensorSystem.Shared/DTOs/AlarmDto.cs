using SNUSSensorSystem.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SNUSSensorSystem.Shared.DTOs
{
    public class AlarmDto
    {
        // sensor on which the alarm sounded off
        public string SensorId { get; set; } = string.Empty;

        // value that caused the alarm
        public double Value { get; set; }

        public AlarmPriority Priority { get; set; }

        public DateTime Timestamp { get; set; }
    }
}
