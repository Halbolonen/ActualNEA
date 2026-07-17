using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlightRouteGenerator
{
    internal class PLNWaypointInfo
    {
        public string ATCWaypointType { get; set; }
        public string WorldPositionLLA { get; set; }
        public string ATCAirway { get; set; }
    }
}
