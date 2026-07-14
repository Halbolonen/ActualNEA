using System;

namespace FlightRouteGenerator
{
    class Program
    {
        public static void Main()
        {
            NavdataInteractor.Initialise();
            WaypointRecord test = (WaypointRecord)NavdataInteractor.waypointRecordDict["270091"];
            Console.WriteLine(test.ident);
            
        }
    }
}