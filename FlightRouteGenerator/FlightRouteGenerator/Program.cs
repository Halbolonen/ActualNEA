using System;

namespace FlightRouteGenerator
{
    class Program
    {
        public static void Main()
        {
            NavdataInteractor.Initialise();
            WaypointRecord test = (WaypointRecord)NavdataInteractor.waypointRecordDict["67"];
            Console.WriteLine(test.ident);
        }
    }
}