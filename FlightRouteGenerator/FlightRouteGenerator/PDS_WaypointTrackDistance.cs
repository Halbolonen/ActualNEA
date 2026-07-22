using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FlightRouteGenerator
{
    internal class PDS_WaypointTrackDistance
    {
        [JsonPropertyName("waypoint_id")]
        public string WaypointID { get; set; }
        [JsonPropertyName("track_distance")]
        public double TrackDistance { get; set; }
    }
}
