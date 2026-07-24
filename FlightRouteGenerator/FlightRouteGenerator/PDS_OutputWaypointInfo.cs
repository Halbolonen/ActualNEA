using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FlightRouteGenerator
{
    internal class PDS_OutputWaypointInfo
    {
        [JsonPropertyName("tas")]
        public double TAS { get; set; }
        [JsonPropertyName("alt")]
        public int Altitude { get; set; }
        [JsonPropertyName("oat")]
        public double OAT { get; set; }
        [JsonPropertyName("mach")]
        public double MachNumber { get; set; }
    }
}
