using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlightRouteGenerator
{
    internal class AStarSearch
    {
        public PriorityQueue<AStarNode, double> openSet { get; set; }
        public Dictionary<string, AStarNode> membersOfOpenSet { get; set; }
        public Dictionary<string, AStarNode> closedSet { get; set; }
        private static AStarNode previousNode { get; set; }
        public double totalDistance { get; private set; }
        public void ExpandOpenSet(AStarNode currentBestNode, AirportRecord arrivalAirport)
        {
            bool airwayFound = false;
            WaypointRecord currentBestWaypoint = currentBestNode.associatedWaypoint;
            List<(WaypointRecord, AirwayRecord)> airwaysFromCurrentBestNode;

            if (NavdataInteractor.outgoingAirwaysByWaypointID.TryGetValue(currentBestWaypoint.WaypointID, out airwaysFromCurrentBestNode))
            {
                foreach ((WaypointRecord, AirwayRecord) airwayTuple in airwaysFromCurrentBestNode)
                {
                    AirwayRecord airwayRecord = airwayTuple.Item2;
                    WaypointRecord newWaypoint = airwayTuple.Item1;
                    // if the airwayRecord does not go to a point that we've already visited,
                    if (!closedSet.ContainsKey(airwayRecord.toWaypointID))
                    {
                        airwayFound = true;
                        AStarNode newNode = new AStarNode();

                        double airwayLength = airwayRecord.length;

                        newNode.gScore = currentBestNode.gScore + airwayLength;

                        void AddToOpenSet()
                        {
                            newNode.hScore = 1000 * Navigator.GetDistanceBetweenGeoCoordinates(newWaypoint.laty, newWaypoint.lonx,
                                arrivalAirport.laty, arrivalAirport.lonx);
                            newNode.UpdateAStarScore();
                            newNode.associatedWaypoint = newWaypoint;

                            openSet.Enqueue(newNode, newNode.aStarScore);

                            membersOfOpenSet[newNode.associatedWaypoint.WaypointID] = newNode;
                        }

                        if (membersOfOpenSet.ContainsKey(newWaypoint.WaypointID))
                        {
                            if (membersOfOpenSet[newWaypoint.WaypointID].gScore > newNode.gScore)
                            {
                                AddToOpenSet();
                            }
                        }
                        else
                        {
                            AddToOpenSet();
                        }
                    }
                }
            }
        }

        public void ExploreOpenSet()
    }
}
