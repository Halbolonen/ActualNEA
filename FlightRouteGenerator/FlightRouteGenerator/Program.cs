using System;

namespace FlightRouteGenerator
{
    class Program
    {
        public static void Main()
        {
            Console.WriteLine("Initialising datasets, please wait...");
            NavdataInteractor.Initialise();
            Console.Clear();

            //Console.WriteLine(NavdataInteractor.outgoingAirwaysByWaypointID["68879"][0].Item1.ident);
            var x = (WaypointRecord)NavdataInteractor.waypointRecordDict["250199"];
            Console.WriteLine(x.ident);
            Console.Write("Welcome to the Flight Route Planner!\nEnter departure airport ICAO code: ");
            string departureInput = Console.ReadLine().ToUpper();
            Console.Write("Enter arrival airport ICAO code: ");
            string arrivalInput = Console.ReadLine().ToUpper();

            Route route = new Route();

            AirportRecord departureAirport = NavdataInteractor.FindAirportByIdent(departureInput);
            AirportRecord arrivalAirport = NavdataInteractor.FindAirportByIdent(arrivalInput);

            route.DepartureAirport = departureAirport;
            route.ArrivalAirport = arrivalAirport;

            AStarSearch aStar = new AStarSearch();
            WaypointRecord closestWaypointToDepartureAirport = Navigator.GetBestUsefullyConnectedWaypointByGeoCoords(departureAirport.laty, departureAirport.lonx, arrivalAirport, new Dictionary<string, AStarNode>());
            AStarNode originNode = new AStarNode();
            originNode.associatedWaypoint = closestWaypointToDepartureAirport;
            originNode.isRoot = true;
            originNode.gScore = 0;
            originNode.hScore = Navigator.GetDistanceBetweenGeoCoordinates(closestWaypointToDepartureAirport.laty, closestWaypointToDepartureAirport.lonx, arrivalAirport.laty, arrivalAirport.lonx);
            originNode.UpdateAStarScore();

            


            AStarNode destinationNode = aStar.ExpandGraphFromNodeUntilDestinationReached(originNode, arrivalAirport);
            route.Legs = aStar.GetRouteToDestinationFromExpandedGraph(destinationNode, departureAirport);

            foreach (RouteLeg leg in route.Legs)
            {
                route.TotalDistance += leg.Length;
            }

            route.TotalDistance += Navigator.GetDistanceBetweenGeoCoordinates(departureAirport.laty, departureAirport.lonx, route.Legs[0].Waypoint.laty, route.Legs[0].Waypoint.lonx);
            

















            Console.WriteLine("\nYour route:\n");
            Console.WriteLine($"DCT {route.DepartureAirport.ident}\n---------------");

            foreach (RouteLeg leg in route.Legs)
            {
                if (leg.Airway.isDirect)
                {
                    leg.Airway.airwayName = "DCT";
                }
                Console.WriteLine($"{leg.Airway.airwayName} {leg.Waypoint.ident} ({Math.Round(leg.Length)} nmi) \n---------------");
            }
            Console.WriteLine($"DCT {route.ArrivalAirport.ident}\n---------------");

            Console.WriteLine($"\n\nRoute in a format suitable for entry into a flight plotting tool:\n\n");
            Console.Write($"{route.DepartureAirport.ident} ");

            foreach (RouteLeg leg in route.Legs)
            {
                if (leg.Airway.isDirect)
                {
                    leg.Airway.airwayName = "DCT";
                }
                Console.Write($"{leg.Airway.airwayName} {leg.Waypoint.ident} ");
            }

            Console.Write($"DCT {route.ArrivalAirport.ident}");

            Console.WriteLine($"\n\nTotal distance: {route.TotalDistance:F1} nmi");
        }
    }
}