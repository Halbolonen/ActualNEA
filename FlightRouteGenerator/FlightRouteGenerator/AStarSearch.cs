using System.Xml;

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
                Console.WriteLine($"empty set at {currentNode.associatedWaypoint.ident}!");
                throw new OpenSetEmptyException();
            }

            //Console.WriteLine($"{newNode.associatedWaypoint.ident}: f: {newNode.aStarScore:F2} g: {newNode.gScore:F2} h: {newNode.hScore:F2}");

            if (newNode.hScore > bestHScoreNode.hScore)
            {
                if (++nonImprovedHScoreIterations == MAX_NON_IMPROVED_H_SCORE_ITERATIONS)
                {
                    Console.WriteLine($"hScore has increased for too long at {newNode.associatedWaypoint.ident}!");
                    throw new RouteDiscontinuityException();
                }
            }
            else
            {
                bestHScoreNode = newNode;
                nonImprovedHScoreIterations = 0;
            }

            if (newNode.gScore > routeGreatCircleDistance * DIRECT_DISTANCE_DISCONTINUITY_CHECK_MULTIPLIER)
            {
                Console.WriteLine($"Route too long at {newNode.associatedWaypoint.ident}!");
                throw new RouteDiscontinuityException();
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
            bool emptyOpenSet = false;

            while (!emptyOpenSet)
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
                catch (RouteDiscontinuityException)
                {
                    currentNode = FindBestNodeToContinueRouteFromAfterDiscontinuity();
                    Console.WriteLine($"Lowest h score node: {currentNode.associatedWaypoint.ident}");

                    AStarNode bestUsefullyConnectedNodeToBestHScoreNode = new AStarNode();
                    try
                    {
                        bestUsefullyConnectedNodeToBestHScoreNode.associatedWaypoint = Navigator.GetBestUsefullyConnectedWaypoint(currentNode.associatedWaypoint, destinationAirport, closedSet);
                    }
                    catch (OpenSetEmptyException)
                    {
                        emptyOpenSet = true;
                        break;
                    }

                    AirwayRecord direct = new AirwayRecord();
                    direct.isDirect = true;
                    direct.length = Navigator.GetDistanceBetweenGeoCoordinates(currentNode.associatedWaypoint.laty,
                        currentNode.associatedWaypoint.lonx,
                        bestUsefullyConnectedNodeToBestHScoreNode.associatedWaypoint.laty,
                        bestUsefullyConnectedNodeToBestHScoreNode.associatedWaypoint.lonx);

                    bestUsefullyConnectedNodeToBestHScoreNode.parent = currentNode;
                    bestUsefullyConnectedNodeToBestHScoreNode.ParentAirway = direct;
                    bestUsefullyConnectedNodeToBestHScoreNode.gScore = currentNode.gScore + direct.length;
                    bestUsefullyConnectedNodeToBestHScoreNode.hScore = Navigator.GetDistanceBetweenGeoCoordinates(bestUsefullyConnectedNodeToBestHScoreNode.associatedWaypoint.laty,
                        bestUsefullyConnectedNodeToBestHScoreNode.associatedWaypoint.lonx,
                        destinationAirport.laty,
                        destinationAirport.lonx);

                    //currentNode = ExpandGraphFromWaypointUntilDestinationReached(Navigator.GetBestUsefullyConnectedWaypoint(bestNode.associatedWaypoint, destinationAirport, closedSet), destinationAirport);
                    currentNode = ExpandGraphFromNodeUntilDestinationReached(bestUsefullyConnectedNodeToBestHScoreNode, destinationAirport);
                    break;
                }

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
            bestHScoreNode = new AStarNode();
            bestHScoreNode.hScore = double.MaxValue;
        }
    }
}
