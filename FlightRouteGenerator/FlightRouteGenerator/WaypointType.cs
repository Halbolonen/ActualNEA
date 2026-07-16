using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlightRouteGenerator
{
    internal enum WaypointType
    {
        Airport = 1,
        NDB = 2,
        VOR = 3,
        NamedFix = 11,
        Coordinate = 28
    }
}
