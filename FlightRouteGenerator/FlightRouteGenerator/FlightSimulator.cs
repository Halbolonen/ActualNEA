using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlightRouteGenerator
{
    internal static class FlightSimulator
    {
        private enum FlightPhases
        {
            Takeoff,
            Climb,
            Cruise,
            Descent,
            FinalApproach
        }
        public static int GetFlightFuelConsumption(int blockFuel, int zfw)
        {
            const double dt = 1;
            // seconds


        }
    }
}
