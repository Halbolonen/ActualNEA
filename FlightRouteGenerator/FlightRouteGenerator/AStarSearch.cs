using System.Xml;
using System.Diagnostics;

namespace FlightRouteGenerator
{
    internal class AStarSearch
    {
        private static int MAX_NON_IMPROVED_H_SCORE_ITERATIONS = 50;
        // maximum number of A* explorations that can occur where the hScore becomes worse before
        // a route discontinuity is declared.

        private static int DIRECT_DISTANCE_DISCONTINUITY_CHECK_MULTIPLIER = 2;
        private AStarNode bestHScoreNode { get; set; }
        private int nonImprovedHScoreIterations = 0;
        private double routeGreatCircleDistance { get; set; }

        public PriorityQueue<AStarNode, double> openSet { get; set; }
        public Dictionary<string, AStarNode> membersOfOpenSet { get; set; }
        public Dictionary<string, AStarNode> closedSet { get; set; }
        public void ExpandOpenSet(AStarNode currentBestNode, AirportRecord arrivalAirport)
        {
            WaypointRecord currentBestWaypoint = currentBestNode.associatedWaypoint;
            List<(WaypointRecord, AirwayRecord)> airwaysFromCurrentBestNode;

            if (NavdataInteractor.outgoingAirwaysByWaypointID.TryGetValue(currentBestWaypoint.WaypointID, out airwaysFromCurrentBestNode))
            {
                foreach ((WaypointRecord, AirwayRecord) airwayTuple in airwaysFromCurrentBestNode)
                {
                    AirwayRecord airwayRecord = airwayTuple.Item2;
                    WaypointRecord newWaypoint = airwayTuple.Item1;
                    bool waypointPresentInASet = false;

                    AStarNode newTemporaryNode = new AStarNode();

                    newTemporaryNode.gScore = currentBestNode.gScore + airwayRecord.length;
                    newTemporaryNode.hScore = Navigator.GetDistanceBetweenGeoCoordinates(newWaypoint.laty, newWaypoint.lonx, arrivalAirport.laty, arrivalAirport.lonx);
                    newTemporaryNode.UpdateAStarScore();


                    if (membersOfOpenSet.TryGetValue(newWaypoint.WaypointID, out AStarNode existingNode))
                    {
                        waypointPresentInASet = true;

                        if (existingNode.gScore > newTemporaryNode.gScore)
                        {

                            existingNode.gScore = newTemporaryNode.gScore;
                            existingNode.hScore = newTemporaryNode.hScore;

                            existingNode.UpdateAStarScore();
                            existingNode.parent = currentBestNode;
                            existingNode.ParentAirway = airwayRecord;

                            openSet.Enqueue(existingNode, existingNode.aStarScore);
                        }
                    }

                    if (closedSet.TryGetValue(newWaypoint.WaypointID, out AStarNode closedNode))
                    {
                        if (newTemporaryNode.gScore < closedNode.gScore)
                        {
                            waypointPresentInASet = true;
                            closedNode.gScore = newTemporaryNode.gScore;
                            closedNode.hScore = newTemporaryNode.hScore;
                            closedNode.UpdateAStarScore();
                            closedNode.parent = currentBestNode;
                            closedNode.ParentAirway = airwayRecord;

                            openSet.Enqueue(closedNode, closedNode.aStarScore);
                            membersOfOpenSet[closedNode.associatedWaypoint.WaypointID] = closedNode;
                            
                            closedSet.Remove(closedNode.associatedWaypoint.WaypointID);
                        }
                        else
                        {
                            waypointPresentInASet = true;
                        }
                    }

                    if (!waypointPresentInASet)
                    {
                        newTemporaryNode.associatedWaypoint = newWaypoint;
                        newTemporaryNode.ParentAirway = airwayRecord;
                        newTemporaryNode.parent = currentBestNode;
                        newTemporaryNode.isRoot = false;

                        openSet.Enqueue(newTemporaryNode, newTemporaryNode.aStarScore);
                        membersOfOpenSet[newTemporaryNode.associatedWaypoint.WaypointID] = newTemporaryNode;
                    }
                }
            }
        }

        public AStarNode ExploreOpenSet(AStarNode currentNode, AirportRecord destination)
        {
            AStarNode newNode = new AStarNode();
            newNode.hScore = double.MaxValue;
            bool newNodeIsMemberOfOpenSet = false;

            while (openSet.Count > 0 && !newNodeIsMemberOfOpenSet)
            {
                newNode = openSet.Dequeue();

                if (membersOfOpenSet.TryGetValue(newNode.associatedWaypoint.WaypointID, out AStarNode confirmedOpenSetMember) && ReferenceEquals(newNode, confirmedOpenSetMember))
                {
                    newNodeIsMemberOfOpenSet = true;
                }
            }

            if (newNodeIsMemberOfOpenSet)
            {
                membersOfOpenSet.Remove(newNode.associatedWaypoint.WaypointID);
                closedSet.Add(newNode.associatedWaypoint.WaypointID, newNode);
            }
            else
            {
                Debug.WriteLine($"empty set at {currentNode.associatedWaypoint.ident}!");
                throw new OpenSetEmptyException();
            }

            return newNode;
        }

        public AStarNode FindBestNodeToContinueRouteFromAfterDiscontinuity()
        {
            openSet.Clear();
            membersOfOpenSet.Clear();
            nonImprovedHScoreIterations = 0;

            return bestHScoreNode;
        }

        public AStarNode ExpandGraphFromNodeUntilDestinationReached(AStarNode originNode, AirportRecord destinationAirport)
        {
            routeGreatCircleDistance = Navigator.GetDistanceBetweenGeoCoordinates(originNode.associatedWaypoint.laty, originNode.associatedWaypoint.lonx, destinationAirport.laty, destinationAirport.lonx);

            AStarNode currentNode = originNode;
            WaypointRecord destinationWaypoint = Navigator.GetClosestWaypointToGeoCoordinates(destinationAirport.laty, destinationAirport.lonx);
            bool emptyOpenSet = false;

            while (currentNode.associatedWaypoint != destinationWaypoint && !emptyOpenSet)
            {
                ExpandOpenSet(currentNode, destinationAirport);
                try
                {
                    currentNode = ExploreOpenSet(currentNode, destinationAirport);
                }
                catch (OpenSetEmptyException)
                {
                    emptyOpenSet = true;
                }

            }

            if (emptyOpenSet)
            {
                throw new RouteDiscontinuityException();
            }

            return currentNode;
        }

        public List<RouteLeg> GetRouteToDestinationFromExpandedGraph(AStarNode currentNode, AirportRecord departureAirport, AirportRecord arrivalAirport)
        {
            List<RouteLeg> route = new List<RouteLeg>();
            double previousgScore = 0;

            void AddLegToRoute()
            {
                previousgScore = currentNode.gScore;

                RouteLeg leg = new RouteLeg();
                leg.Waypoint = currentNode.associatedWaypoint;
                leg.Airway = currentNode.ParentAirway;

                route.Add(leg);
                currentNode = currentNode.parent;
            }

            AddLegToRoute();

            WaypointRecord finalWaypoint = route[0].Waypoint;

            double distanceFromFinalWaypointToArrivalAirport =
                Navigator.GetDistanceBetweenGeoCoordinates(finalWaypoint.laty, finalWaypoint.lonx,
                arrivalAirport.laty, arrivalAirport.lonx);

            RouteLeg finalWptToDestinationLeg = new RouteLeg();
            WaypointRecord arrivalAirportDummy = new WaypointRecord();
            arrivalAirportDummy.ident = arrivalAirport.ident;
            finalWptToDestinationLeg.isAirportLeg = true;

            finalWptToDestinationLeg.Airway = AirwayRecord.CreateDirectBetweenGeoCoordinates(finalWaypoint.laty, finalWaypoint.lonx,
    arrivalAirport.laty, arrivalAirport.lonx);
            finalWptToDestinationLeg.Waypoint = arrivalAirportDummy;

            RouteLeg finalStandardLeg = route[0];
            route.Remove(finalStandardLeg);

            route.Add(finalWptToDestinationLeg);

            route.Add(finalStandardLeg);

            while (!currentNode.isRoot)
            {
                AddLegToRoute();
            }

            RouteLeg depArptToFirstWptLeg = new RouteLeg();
            AirwayRecord depArptToFirstWptArwy = AirwayRecord.CreateDirectBetweenGeoCoordinates(
                departureAirport.laty, departureAirport.lonx,
                currentNode.associatedWaypoint.laty, currentNode.associatedWaypoint.lonx);

            depArptToFirstWptLeg.Waypoint = currentNode.associatedWaypoint;
            depArptToFirstWptLeg.Airway = depArptToFirstWptArwy;
            depArptToFirstWptLeg.isAirportLeg = true;

            route.Add(depArptToFirstWptLeg);

            route.Reverse();

            return route;
        }

        public static AStarNode CreateOriginNode(AirportRecord departureAirport, AirportRecord arrivalAirport)
        {
            AStarNode originNode = new AStarNode();
            WaypointRecord closestWaypointToDepartureAirport = Navigator.GetClosestWaypointToGeoCoordinates(departureAirport.laty,
                departureAirport.lonx);
            originNode.associatedWaypoint = closestWaypointToDepartureAirport;
            originNode.isRoot = true;
            originNode.gScore = 0;
            originNode.hScore = Navigator.GetDistanceBetweenGeoCoordinates(closestWaypointToDepartureAirport.laty, closestWaypointToDepartureAirport.lonx, arrivalAirport.laty, arrivalAirport.lonx);
            originNode.UpdateAStarScore();

            return originNode;
        }

        public Route GetRouteBetweenAirports(AirportRecord departureAirport, AirportRecord arrivalAirport)
        {
            Route route = new Route();

            AStarNode originNode = CreateOriginNode(departureAirport, arrivalAirport);

            route.DepartureAirport = departureAirport;
            route.ArrivalAirport = arrivalAirport;
            route.TotalDistance = 0;

            AStarNode destinationNode;

            try
            {
                destinationNode = ExpandGraphFromNodeUntilDestinationReached(originNode, arrivalAirport);
            }
            catch (RouteDiscontinuityException)
            {
                throw;
            }


            route.Legs = GetRouteToDestinationFromExpandedGraph(destinationNode, departureAirport, arrivalAirport);

            foreach (RouteLeg leg in route.Legs)
            {
                route.TotalDistance += leg.Length;

                if (leg.Airway.isDirect)
                {
                    // TODO: move this logic to the console printing method.
                    //leg.Airway.airwayName = GLOBAL_SETTINGS.DIRECT_FORMAT;
                }
            }

            route.enrouteWaypointCount = route.Legs.Count - 1;

            return route;
        }

        public AStarSearch()
        {
            openSet = new PriorityQueue<AStarNode, double>();
            membersOfOpenSet = new Dictionary<string, AStarNode>();
            closedSet = new Dictionary<string, AStarNode>();
            bestHScoreNode = new AStarNode();
            bestHScoreNode.hScore = double.MaxValue;
        }
    }
}
