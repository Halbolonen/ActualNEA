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
        [JsonPropertyName("cruise_alt")]
        public double CruiseAltitude { get; set; }
        [JsonPropertyName("waypoint_id_to_info")]
        public Dictionary<string, PDS_WaypointInfo> WaypointIDToInfo { get; set; }
    }
}
