using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlightRouteGenerator
{
    internal class AStarNode
    {
        public WaypointRecord associatedWaypoint { get; set; }
        public double gScore { get; set; }
        public double aStarScore { get; private set; }
        public double hScore { get; set; }
        public AStarNode parent { get; set; }
        public bool isRoot { get; set; }

        public void UpdateAStarScore()
        {
            aStarScore = gScore + hScore;
        }

        public AStarNode()
        {
            isRoot = false;
        }
    }
}
