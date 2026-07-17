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

                Console.Clear();
                Console.WriteLine("Use the menu to select the formats you want your flight plan to be outputted in.\n");

                List<string> outputOptions = new List<string> {"Console","PDF File","X-Plane route file (.fms)","Microsoft Flight Simulator route file (.pln)"};
                HashSet<int> choices = MultipleChoiceMenu.GetUserChoice(outputOptions);

                foreach (int choice in choices)
                {
                    switch (choice)
                    {
                        case 0:
                            PlanOutputManager.OutputRouteToConsole(route);
                            break;

                        case 1:
                            Console.WriteLine("not implemented");
                            break;

                        case 2:
                            PlanOutputManager.OutputRouteToFMSFile(route);
                            break;

                        case 3:
                            PlanOutputManager.OutputRouteToPLNFile(route);
                            break;
                    }
                }
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