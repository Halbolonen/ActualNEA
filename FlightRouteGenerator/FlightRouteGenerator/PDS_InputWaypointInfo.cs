using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FlightRouteGenerator
{
    internal class PDS_InputWaypointInfo
    {
        [JsonPropertyName("waypoint_id")]
        public string WaypointID { get; set; }
        [JsonPropertyName("track_distance")]
        public double TrackDistance { get; set; }
        [JsonPropertyName("latitude")]
        public double laty { get; set; }
        [JsonPropertyName("longitude")]
        public double lonx { get; set; }
    }
}
