using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FlightRouteGenerator
{
    internal class PDS_AltitudeRequest
    {
        [JsonPropertyName("altitude")]
        public double Altitude { get; set; }
    }
}
