using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlightRouteGenerator
{
    internal class LoadsheetInfo
    {
        public int Pax { get; set; }
        // number of passengers
        public int BlockFuel { get; set; }
        // total weight of fuel loaded onto the aircraft, in kilograms
        public int BagsAndCargo { get; set; }
        // checked bags and cargo loaded onto the aircraft, in kilograms
        public int Payload { get; set; }
        // passengers + carry-on + checked bags and cargo, in kilograms
        public int TOW { get; set; }
        // Take-Off Weight, in kilograms
        public int LAW { get; set; }
        // Landing Weight, in kilograms
        public int ZFW { get; set; }
        // Zero Fuel Weight, in kilograms
    }
}
