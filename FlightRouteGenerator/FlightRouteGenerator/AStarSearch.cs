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

                    AStarNode newTemporaryNode = new AStarNode();

                    newTemporaryNode.gScore = currentBestNode.gScore + airwayRecord.length;
                    newTemporaryNode.hScore = Navigator.GetDistanceBetweenGeoCoordinates(newWaypoint.laty, newWaypoint.lonx, arrivalAirport.laty, arrivalAirport.lonx);
                    newTemporaryNode.UpdateAStarScore();


                    if (membersOfOpenSet.TryGetValue(newWaypoint.WaypointID, out AStarNode existingNode))
                    {
                        if (existingNode.aStarScore > newTemporaryNode.aStarScore)
                        {
                            existingNode.gScore = newTemporaryNode.gScore;
                            existingNode.hScore = newTemporaryNode.hScore;

                            existingNode.UpdateAStarScore();
                        }
                    }

                    if (closedSet.TryGetValue(newWaypoint.WaypointID, out AStarNode closedNode))
                    {
                        //transfer to open set if new temporary node generated from new waypoint has a better ass than the closedNode.
                    }

                    /*if (!closedSet.ContainsKey(airwayRecord.toWaypointID))
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
                    }*/


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
                Console.WriteLine(newNode.associatedWaypoint.ident);

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
                throw new Exception("no possible routes");
            }

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
