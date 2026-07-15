using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace FlightRouteGenerator
{
    internal static class Navigator
    {
        public static double GetDistanceBetweenGeoCoordinates(double orgLaty, double orgLonx, double dstLaty, double dstLonx)
        {
            const double DEG_TO_RAD = Math.PI / 180;
            const int r = 3440;
            // earth radius in nmi

            double phi1 = orgLaty * DEG_TO_RAD;
            double phi2 = dstLaty * DEG_TO_RAD;
            double lambda1 = orgLonx * DEG_TO_RAD;
            double lambda2 = dstLonx * DEG_TO_RAD;

            // using the haversine formula to find great circle distance between geographical coordinates
            double h = Math.Pow(Math.Sin((phi2 - phi1) / 2), 2) + Math.Cos(phi1) * Math.Cos(phi2) * Math.Pow(Math.Sin((lambda2 - lambda1) / 2), 2);

            return 2 * r * Math.Asin(Math.Sqrt(h));
        }

        public static WaypointRecord GetBestUsefullyConnectedWaypoint(WaypointRecord currentWaypoint, AirportRecord destination, 
            Dictionary<string, AStarNode> closedSet)
        {
            WaypointRecord waypointToReturn = new WaypointRecord();
            bool waypointFound = false;
            List<WaypointRecord> candidates = new List<WaypointRecord>();

            foreach (string waypointID in NavdataInteractor.outgoingAirwaysByWaypointID.Keys)
            {
                WaypointRecord testWaypoint = (WaypointRecord)NavdataInteractor.waypointRecordDict[waypointID];
                // firstly, is it closer to the destination?
                double distanceFromTestWaypointToDestination = Navigator.GetDistanceBetweenGeoCoordinates(
                    testWaypoint.laty, testWaypoint.lonx, destination.laty, destination.lonx);
                double distanceFromCurrentWaypointToDestination = Navigator.GetDistanceBetweenGeoCoordinates(
                    currentWaypoint.laty, currentWaypoint.lonx, destination.laty, destination.lonx);

                if (distanceFromTestWaypointToDestination < distanceFromCurrentWaypointToDestination)
                {
                    // then, does it have any non-explored airways that will lead us closer to the destination?
                    int suitableNonExploredAirwaysCount = 0;
                    List<(WaypointRecord, AirwayRecord)> outgoingAirwaysFromTestWaypoint;

                    if (NavdataInteractor.outgoingAirwaysByWaypointID.TryGetValue(testWaypoint.WaypointID, out outgoingAirwaysFromTestWaypoint))
                    {
                        foreach ((WaypointRecord, AirwayRecord) airwayTuple in outgoingAirwaysFromTestWaypoint)
                        {
                            WaypointRecord tupleWaypoint = airwayTuple.Item1;
                            if (!closedSet.TryGetValue(tupleWaypoint.WaypointID, out AStarNode airwayDestinationNode))
                            {
                                if (Navigator.GetDistanceBetweenGeoCoordinates(
                                    tupleWaypoint.laty, tupleWaypoint.lonx, destination.laty, destination.lonx)
                                    < Navigator.GetDistanceBetweenGeoCoordinates(
                                        currentWaypoint.laty, currentWaypoint.lonx, destination.laty, destination.lonx))
                                {
                                    suitableNonExploredAirwaysCount++;
                                }
                            }
                        }
                    }

                    if (suitableNonExploredAirwaysCount > 0)
                    {
                        // if it does, store it in a list.
                        waypointFound = true;
                        candidates.Add(testWaypoint);
                    }
                }
            }

            double shortestDistanceToCurrentWaypoint = double.MaxValue;

            foreach(WaypointRecord waypointCandidate in candidates)
            {
                double distanceToCurrentWaypoint = Navigator.GetDistanceBetweenGeoCoordinates(currentWaypoint.laty, currentWaypoint.lonx,
                    waypointCandidate.laty, waypointCandidate.lonx);
                if (distanceToCurrentWaypoint < shortestDistanceToCurrentWaypoint)
                {
                    shortestDistanceToCurrentWaypoint = distanceToCurrentWaypoint;
                    waypointToReturn = waypointCandidate;
                }
            }

            if (!waypointFound)
            {
                throw new Exception("couldn't find a backup!!! your code seriously sucks dude.");
            }

            return waypointToReturn;
        }

        public static WaypointRecord GetClosestWaypointToGeoCoordinates(double currentLaty, double currentLonx)
        {
            WaypointRecord closestWaypoint = new WaypointRecord();
            double shortestDistance = double.MaxValue;

            foreach (WaypointRecord waypoint in NavdataInteractor.waypointRecordDict.Values)
            {
                double distance = Navigator.GetDistanceBetweenGeoCoordinates(currentLaty, currentLonx, waypoint.laty, waypoint.lonx);

                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    closestWaypoint = waypoint;
                }
            }

            return closestWaypoint;
        }
    }
}
