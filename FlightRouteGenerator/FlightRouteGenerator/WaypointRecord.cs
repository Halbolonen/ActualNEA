using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlightRouteGenerator
{
    internal class WaypointRecord : Record
    {
        public string ident { get; set; }
        public double lonx { get; set; }
        public double laty { get; set; }
        private string waypointID;
        public int Type { get; set; }
        public int Altitude { get; set; }
        // in feet
        public double TAS { get; set; }
        // True AirSpeed for the aircraft to reach at this waypoint, in knots
        public string Name { get; set; }
        
        public string WaypointID {
            get
            {
                return waypointID;
            }
            set 
            {
                primaryKey = value;
                waypointID = value;
            } 
        }

        public WaypointRecord()
        {
            laty = 0;
            lonx = 0;
        }
    }
}
