using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlightRouteGenerator
{
    internal class RouteLeg
    {
        public WaypointRecord Waypoint { get; set; }
        private AirwayRecord airway;
        public AirwayRecord Airway
        {
            get
            {
                return airway;
            }
            
            set
            {
                airway = value;
                Length = airway.length;
            }
        }
        public double Length { get; set; }
    }
}
