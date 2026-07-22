using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FlightRouteGenerator
{
    internal class PDS_FuelFlowParameters
    {
        public int mass { get; set; }
        public int tas { get; set; }
        public int alt { get; set; }
        public int vs { get; set; }
        public int acc { get; set; }
        public int dT { get; set; }
        // this is ISA deviation, NOT delta time!
        public string aircraft_type { get; set; }

        public PDS_FuelFlowParameters()
        {
            acc = 0;
            dT = 0;
        }
    }
}
