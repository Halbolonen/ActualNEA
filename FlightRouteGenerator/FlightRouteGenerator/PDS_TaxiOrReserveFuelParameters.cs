using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FlightRouteGenerator
{
    internal class PDS_TaxiOrReserveFuelParameters
    {
        [JsonPropertyName("mass")]
        public int Mass { get; set; }
        [JsonPropertyName("tas")]
        public double TAS{ get; set; }
        [JsonPropertyName("alt")]
        public int Altitude { get; set; }
        [JsonPropertyName("time")]
        public int Time { get; set; }
        [JsonPropertyName("aircraft_type")]
        public string AircraftType { get; set; }
    }
}
