using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SNUSSensorSystem.Shared.Enums
{
    // The quality of the data the sensor produces
    public enum DataQuality
    {
        Good = 0, // reliable data
        Bad = 1, // bad data, this status is given to the sensor that is also considered malicious
        Uncertain = 2 // is not treated as a reliable data source
    }
}
