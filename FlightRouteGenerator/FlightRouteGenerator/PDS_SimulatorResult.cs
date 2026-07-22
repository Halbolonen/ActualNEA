using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FlightRouteGenerator
{
    internal class PDS_SimulatorResult
    {
        [JsonPropertyName("trip_fuel")]
        public double TripFuel { get; set; }
        [JsonPropertyName("waypoint_id_to_alt")]
        public Dictionary<string, int> WaypointIDToAlt { get; set; }
    }
}
