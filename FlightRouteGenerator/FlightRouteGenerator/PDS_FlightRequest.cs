using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FlightRouteGenerator
{
    internal class PDS_FlightRequest
    {
        [JsonPropertyName("departure_arprt_alt")]
        public int DepartureAirportAltitude { get; set; }
        [JsonPropertyName("arrival_arprt_alt")]
        public int ArrivalAirportAltitude { get; set; }
        [JsonPropertyName("acft_request")]
        public PDS_AircraftRequest AircraftRequest { get; set; }
        [JsonPropertyName("zfw")]
        public double ZFW { get; set; }
        [JsonPropertyName("cruise_altitude")]
        public int CruiseAltitude { get; set; }
        [JsonPropertyName("route_total_distance")]
        public double RouteTotalDistance { get; set; }
        [JsonPropertyName("trip_fuel")]
        public double TripFuel { get; set; }
        [JsonPropertyName("waypoint_id_to_track_distance")]
        public List<PDS_WaypointTrackDistance> WaypointIDToTrackDistance { get; set; }
    }
}
