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
        private static readonly double DEG_TO_RAD = Math.PI / 180;
        private static readonly int EARTH_RADIUS = 3440;
        // in nautical miles
        public static double GetDistanceBetweenGeoCoordinates(double orgLaty, double orgLonx, double dstLaty, double dstLonx)
        {

            double phi1 = orgLaty * DEG_TO_RAD;
            double phi2 = dstLaty * DEG_TO_RAD;
            double lambda1 = orgLonx * DEG_TO_RAD;
            double lambda2 = dstLonx * DEG_TO_RAD;

            // using the haversine formula to find great circle distance between geographical coordinates
            double h = Math.Pow(Math.Sin((phi2 - phi1) / 2), 2) + Math.Cos(phi1) * Math.Cos(phi2) * Math.Pow(Math.Sin((lambda2 - lambda1) / 2), 2);

            return 2 * EARTH_RADIUS * Math.Asin(Math.Sqrt(h));
        }

        public static double GetBearingBetweenGeoCoordinates(double orgLaty, double orgLonx, double dstLaty, double dstLonx)
        {
            const double RAD_TO_DEG = 180 / Math.PI;
            double phi1 = orgLaty * DEG_TO_RAD;
            double phi2 = dstLaty * DEG_TO_RAD;
            double lambda1 = orgLonx * DEG_TO_RAD;
            double lambda2 = dstLonx * DEG_TO_RAD;

            double angle = Math.Atan2(Math.Sin(lambda2 - lambda1) * Math.Cos(phi2), Math.Cos(phi1) * Math.Sin(phi2) - Math.Sin(phi1) * Math.Cos(phi2) * Math.Cos(lambda2 - lambda1));
            double bearing = (RAD_TO_DEG * angle + 360) % 360;

            return bearing;
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
