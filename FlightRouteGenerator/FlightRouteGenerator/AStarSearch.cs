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
                throw new MustGoDirectToDestinationException();
            }

            Console.WriteLine($"{newNode.associatedWaypoint.ident}: f: {newNode.aStarScore:F2} g: {newNode.gScore:F2} h: {newNode.hScore:F2}");

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
            bool mustGoDirectToDestinationNow = false;

            while (!mustGoDirectToDestinationNow)
            {
                ExpandOpenSet(currentNode, destinationAirport);
                try
                {
                    currentNode = ExploreOpenSet(currentNode, destinationAirport);
                }
                catch (MustGoDirectToDestinationException)
                {
                    mustGoDirectToDestinationNow = true;
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
            totalDistance = 0;
        }
    }
}
