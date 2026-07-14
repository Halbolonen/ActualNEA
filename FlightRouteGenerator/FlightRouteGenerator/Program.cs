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
            Console.WriteLine(test.ident);
            Console.WriteLine(Navigator.GetBestUsefullyConnectedWaypoint(test, destTest, new Dictionary<string, AStarNode>()).ident);
        }
    }
}