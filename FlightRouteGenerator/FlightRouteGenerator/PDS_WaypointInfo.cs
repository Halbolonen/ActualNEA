using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FlightRouteGenerator
{
    internal class PDS_WaypointInfo
    {
        [JsonPropertyName("tas")]
        public double TAS { get; set; }
        [JsonPropertyName("alt")]
        public double Altitude { get; set; }
    }
}
