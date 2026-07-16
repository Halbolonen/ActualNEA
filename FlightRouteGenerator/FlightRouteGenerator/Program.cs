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

            Console.Write("Welcome to the Flight Route Planner!\nEnter departure airport ICAO code: ");
            string departureInput = Console.ReadLine().ToUpper();
            Console.Write("Enter arrival airport ICAO code: ");
            string arrivalInput = Console.ReadLine().ToUpper();

            AirportRecord departureAirport = NavdataInteractor.FindAirportByIdent(departureInput);
            AirportRecord arrivalAirport = NavdataInteractor.FindAirportByIdent(arrivalInput);

            AStarSearch aStar = new AStarSearch();
            Route route = aStar.GetRouteBetweenAirports(departureAirport, arrivalAirport);

            PlanOutputManager.OutputRouteToConsole(route);
            PlanOutputManager.OutputRouteToFMSFile(route);
        }
    }
}