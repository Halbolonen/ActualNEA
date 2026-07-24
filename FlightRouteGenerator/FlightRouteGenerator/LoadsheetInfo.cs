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
        public double BlockFuel { get; set; }
        // total weight of fuel loaded onto the aircraft, in kilograms
        public double BagsAndCargo { get; set; }
        // checked bags and cargo loaded onto the aircraft, in kilograms
        public double Payload { get; set; }
        // passengers + carry-on + checked bags and cargo, in kilograms
        public double TOW { get; set; }
        // Take-Off Weight, in kilograms
        public double LAW { get; set; }
        // Landing Weight, in kilograms
        public double ZFW { get; set; }
        // Zero Fuel Weight, in kilograms
        public double FinalReserveFuel { get; set; }
        // in kilograms
        public double TripFuel { get; set; }
        // in kilograms
        public double TaxiFuel { get; set; }
        // in kilograms
    }
}
