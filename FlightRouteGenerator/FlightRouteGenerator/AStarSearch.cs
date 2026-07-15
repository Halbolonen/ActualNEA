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
                            newNode.gScore = currentBestNode.gScore + airwayLength;
                            newNode.hScore = Navigator.GetDistanceBetweenGeoCoordinates(newWaypoint.laty, newWaypoint.lonx,
                                arrivalAirport.laty, arrivalAirport.lonx);
                            newNode.UpdateAStarScore();
                            newNode.associatedWaypoint = newWaypoint;
                            newNode.isRoot = false;
                            newNode.parent = currentBestNode;

                            if (!currentBestNode.isProductOfRouteDiscontinuity)
                            {
                                newNode.ParentAirway = airwayRecord;
                            }
                            else
                            {
                                AirwayRecord direct = new AirwayRecord();
                                direct.length = Navigator.GetDistanceBetweenGeoCoordinates(currentBestWaypoint.laty, currentBestWaypoint.lonx, newWaypoint.laty, newWaypoint.lonx);
                                direct.isDirect = true;
                                newNode.ParentAirway = direct;
                            }

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

        public AStarNode ExploreOpenSet(AStarNode currentNode, AirportRecord destination)
        {
            WaypointRecord newWaypoint = new WaypointRecord();
            AStarNode newNode = new AStarNode();
            newNode.hScore = double.MaxValue;

            while (openSet.Count > 0 && newNode.hScore > currentNode.hScore)
            {
                newNode = openSet.Dequeue();
            }

            if (openSet.Count == 0 && newNode.hScore > currentNode.hScore)
            {
                // panic! there are no more options in the open set
                // that get us closer to the destination. cheating time!
                AStarNode prizeNode = new AStarNode();
                prizeNode.hScore = double.MaxValue;
                bool prizeNodeDefined = false;

                foreach (AStarNode node in closedSet.Values)
                {
                    if (node.hScore < prizeNode.hScore)
                    {
                        prizeNode = node;
                        prizeNodeDefined = true;
                    }
                }

                if (!prizeNodeDefined)
                {
                    prizeNode = currentNode;
                }

                WaypointRecord bestUsefullyConnectedWaypoint = Navigator.GetBestUsefullyConnectedWaypoint(prizeNode.associatedWaypoint, destination,
                    closedSet);

                AStarNode bestUsefullyConnectedNode = new AStarNode();

                bestUsefullyConnectedNode.gScore = prizeNode.gScore + Navigator.GetDistanceBetweenGeoCoordinates(
                    prizeNode.associatedWaypoint.laty, prizeNode.associatedWaypoint.lonx,
                    bestUsefullyConnectedWaypoint.laty, bestUsefullyConnectedWaypoint.lonx);

                bestUsefullyConnectedNode.hScore = Navigator.GetDistanceBetweenGeoCoordinates(
                    bestUsefullyConnectedWaypoint.laty, bestUsefullyConnectedWaypoint.lonx,
                    destination.laty, destination.lonx);

                bestUsefullyConnectedNode.parent = prizeNode;
                bestUsefullyConnectedNode.associatedWaypoint = bestUsefullyConnectedWaypoint;
                bestUsefullyConnectedNode.UpdateAStarScore();
                bestUsefullyConnectedNode.isProductOfRouteDiscontinuity = true;

                AirwayRecord direct = new AirwayRecord();
                direct.length = Navigator.GetDistanceBetweenGeoCoordinates(prizeNode.associatedWaypoint.laty, prizeNode.associatedWaypoint.lonx, bestUsefullyConnectedWaypoint.laty, bestUsefullyConnectedWaypoint.lonx);
                direct.isDirect = true;
                bestUsefullyConnectedNode.ParentAirway = direct;

                ExpandOpenSet(bestUsefullyConnectedNode, destination);

                if (openSet.Count == 0)
                {
                    //
                    throw new Exception($"i'm panicking, no i am because i'm gonna lose me route\n{bestUsefullyConnectedWaypoint.ident}");
                }

                newNode = openSet.Dequeue();
            }

            AStarNode parent = newNode.parent;

            newNode.gScore = parent.gScore + Navigator.GetDistanceBetweenGeoCoordinates(parent.associatedWaypoint.laty, parent.associatedWaypoint.lonx, 
                newNode.associatedWaypoint.laty, newNode.associatedWaypoint.lonx);

            newNode.UpdateAStarScore();

            return newNode;
        }

        public AStarNode ExpandGraphFromWaypointUntilDestinationReached(WaypointRecord originWaypoint, AirportRecord destinationAirport)
        {
            AStarNode originNode = new AStarNode();
            originNode.associatedWaypoint = originWaypoint;
            originNode.gScore = 0;
            originNode.hScore = Navigator.GetDistanceBetweenGeoCoordinates(originWaypoint.laty, originWaypoint.lonx, destinationAirport.laty, destinationAirport.lonx);
            originNode.UpdateAStarScore();
            originNode.isRoot = true;
            originNode.parent = originNode;

            AStarNode currentNode = originNode;

            while (currentNode.hScore > GLOBAL_SETTINGS.MAX_DIST_FROM_DEST)
            {
                ExpandOpenSet(currentNode, destinationAirport);
                currentNode = ExploreOpenSet(currentNode, destinationAirport);
                //Console.WriteLine(currentNode.associatedWaypoint.ident);
            }
            return currentNode;
        }

        public List<RouteLeg> GetRouteToDestinationFromExpandedGraph(AStarNode currentNode, AirportRecord departureAirport)
        {
            List<RouteLeg> route = new List<RouteLeg>();
            double previousgScore = 0;

            while (!currentNode.isRoot)
            {
                previousgScore = currentNode.gScore;

                RouteLeg leg = new RouteLeg();
                leg.Waypoint = currentNode.associatedWaypoint;
                if (currentNode.ParentAirway == null)
                {
                    Console.WriteLine(currentNode.associatedWaypoint.ident);
                    Console.WriteLine(currentNode.isProductOfRouteDiscontinuity);
                }
                leg.Airway = currentNode.ParentAirway;

                route.Add(leg);
                 currentNode = currentNode.parent;
            }

            RouteLeg finalStep = new RouteLeg();
            AirwayRecord finalAirway = new AirwayRecord();

            finalAirway.isDirect = true;
            finalAirway.length = Navigator.GetDistanceBetweenGeoCoordinates(departureAirport.laty, departureAirport.lonx, currentNode.associatedWaypoint.laty, currentNode.associatedWaypoint.lonx);

            finalStep.Waypoint = currentNode.associatedWaypoint;
            finalStep.Airway = finalAirway;

            route.Add(finalStep);

            route.Reverse();

            return route;
        }

        public AStarSearch()
        {
            openSet = new PriorityQueue<AStarNode, double>();
            membersOfOpenSet = new Dictionary<string, AStarNode>();
            closedSet = new Dictionary<string, AStarNode>();
            totalDistance = 0;
        }
    }
}
