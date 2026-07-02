using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SNUSSensorSystem.Shared.Models
{
    public class ConsensusValue
    {
        public long Id { get; set; }

        // calculated consensus temp for given min
        public double Value { get; set; }

        // min whose data the algorithm read
        public DateTime Timestamp { get; set; }

        // moment when the consensus is calculated and saved
        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

        // how many sensors (quality Good) was used in calculating consensus
        // useful for diagnostics BFT
        public int ParticipatingSensorCount { get; set; }

        public bool IsConsensus { get; set; } = true;
    }
}
