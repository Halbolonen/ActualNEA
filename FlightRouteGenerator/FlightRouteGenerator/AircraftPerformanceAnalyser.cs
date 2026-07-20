using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlightRouteGenerator
{
    internal static class AircraftPerformanceAnalyser
    {
        public static HashSet<string> SupportedAircraftTypes { get; private set; }

        public static Route AddVerticalProfileToRoute()
        {
            throw new NotImplementedException();
        }

        static AircraftPerformanceAnalyser()
        {
            SupportedAircraftTypes = new HashSet<string> { "A320" };
        }
    }
}
