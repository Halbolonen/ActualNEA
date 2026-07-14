using System;

namespace FlightRouteGenerator
{
    class Program
    {
        public static void Main()
        {
            NavdataInteractor.Initialise();
            WaypointRecord test = (WaypointRecord)NavdataInteractor.waypointRecordDict["270091"];
            AirportRecord destTest = (AirportRecord)NavdataInteractor.airportRecordDict["9913"];

            AStarSearch aStar = new AStarSearch();
            aStar.ExpandGraphFromWaypointUntilDestinationReached(test, destTest);
        }
    }
}