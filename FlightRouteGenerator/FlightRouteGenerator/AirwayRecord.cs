using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlightRouteGenerator
{
    internal class AirwayRecord : Record
    {
        private string airwayID;
        public string AirwayID
        {
            get
            {
                return airwayID;
            }
            set
            {
                primaryKey = value;
                airwayID = value;
            }
        }
        public string airwayName { get; set; }
        public string fromWaypointID { get; set; }
        public string toWaypointID { get; set; }
        public double fromLaty { get; set; }
        public double fromLonx { get; set; }
        public double toLaty { get; set; }
        public double toLonx { get; set; }
        public double length { get; set; }
        public double aStarScore { get; set; }
    }
}
