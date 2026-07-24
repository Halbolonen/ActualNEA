using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FlightRouteGenerator
{
    internal class PDS_TCorTDInfo
    {
        [JsonPropertyName("latitude")]
        public double laty { get; set; }
        [JsonPropertyName("longitude")]
        public double lonx { get; set; }
        [JsonPropertyName("altitude")]
        public double Altitude { get; set; }
        [JsonPropertyName("tas")]
        public double TAS { get; set; }
        [JsonPropertyName("mach")]
        public double Mach { get; set; }
        [JsonPropertyName("oat")]
        public double OAT { get; set; }
        [JsonPropertyName("previous_leg_index")]
        public double PreviousLegIndex { get; set; }
    }
}
