using System;
using System.Threading;

namespace FlightRouteGenerator
{
    class Program
    {
        private static void RunProgram()
        {
            Console.WriteLine("Initialising datasets, please wait...");
            NavdataInteractor.Initialise();
            Console.Clear();

            Console.Write("Welcome to the Flight Route Planner!\nEnter departure airport ICAO code: ");
            string departureInput = Console.ReadLine().ToUpper();
            Console.Write("Enter arrival airport ICAO code: ");
            string arrivalInput = Console.ReadLine().ToUpper();

            if (departureInput == arrivalInput)
            {
                throw new InvalidRouteInputException();
            }
            AirportRecord departureAirport;
            AirportRecord arrivalAirport;

            try
            {
                departureAirport = NavdataInteractor.FindAirportByIdent(departureInput);
                arrivalAirport = NavdataInteractor.FindAirportByIdent(arrivalInput);
            }
            catch (AirportNotFoundByIdentException)
            {
                throw new InvalidRouteInputException();
            }

            AStarSearch aStar = new AStarSearch();
            Route route;

            try
            {
                route = aStar.GetRouteBetweenAirports(departureAirport, arrivalAirport);
                PlanOutputManager.OutputRouteToConsole(route);
                PlanOutputManager.OutputRouteToFMSFile(route);
            }
            catch (RouteDiscontinuityException)
            {
                Console.WriteLine($"\nUnfortunately, no route could be found between {departureAirport.ident} and {arrivalAirport.ident}.");
            }
        }

        private static void StartProgram()
        {
            try
            {
                RunProgram();
            }
            catch (InvalidRouteInputException)
            {
                Console.WriteLine("\n\nInvalid input.\nOnly enter different valid ICAO airport codes.\nPress any key to restart...");
                Console.ReadKey();
                Console.Clear();
                StartProgram();
            }
        }

        public static void Main()
        {
            StartProgram();

            Console.WriteLine("\n\nPress any key to exit.");
            Console.ReadKey();
        }
    }
}