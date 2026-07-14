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
            double phi1 = orgLaty;
            double phi2 = dstLaty;
            double lambda1 = orgLonx;
            double lambda2 = dstLonx;
            const int r = 3444;
            // earth radius in nmi

            // using the haversine formula to find great circle distance between geographical coordinates
            double h = Math.Pow(Math.Sin((phi2 - phi1) / 2), 2) + Math.Cos(phi1) * Math.Cos(phi2) * Math.Pow(Math.Sin((lambda2 - lambda1) / 2), 2);

            return 2 * r * Math.Pow(Math.Sin(Math.Sqrt(h)), -1);
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
                    int nonExploredAirwaysCount = 0;
                    List<(WaypointRecord, AirwayRecord)> outgoingAirwaysFromTestWaypoint;

                    if (NavdataInteractor.outgoingAirwaysByWaypointID.TryGetValue(currentWaypoint.WaypointID, out outgoingAirwaysFromTestWaypoint))
                    {
                        foreach ((WaypointRecord, AirwayRecord) airwayTuple in outgoingAirwaysFromTestWaypoint)
                        {

                            if (!closedSet.TryGetValue(airwayTuple.Item1.WaypointID, out AStarNode airwayDestinationNode))
                            {
                                nonExploredAirwaysCount++;
                            }
                        }
                    }

                    if (nonExploredAirwaysCount > 0)
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
    }
}
