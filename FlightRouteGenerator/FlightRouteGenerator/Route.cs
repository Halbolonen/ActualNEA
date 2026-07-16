using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlightRouteGenerator
{
    internal class Route
    {
        public List<RouteLeg> Legs { get; set; }
        public AirportRecord DepartureAirport { get; set; }
        public AirportRecord ArrivalAirport { get; set; }
        public double TotalDistance { get; set; }
        public int enrouteWaypointCount { get; set; }

        public Route()
        {
            enrouteWaypointCount = 0;
        }
    }
}
