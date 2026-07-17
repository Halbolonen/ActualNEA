using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlightRouteGenerator
{
    internal class AirportRecord : Record
    {
        public string ident { get; set; }
        public string name { get; set; }
        public double lonx { get; set; }
        public double laty { get; set; }
        public int altitude { get; set; }
        // in ft

        private string airportID;
        public string AirportID
        {
            get
            {
                return airportID;
            }
            set
            {
                primaryKey = value;
                airportID = value;
            }
        }
    }
}
