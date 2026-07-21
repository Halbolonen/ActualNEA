using System;
using System.Text.Json;
using System.Threading;

namespace FlightRouteGenerator
{
    class Program
    {
        private static async Task RunProgram()
        {
            if (!NavdataInteractor.Initialised)
            {
                Console.Write("Initialising datasets, please wait...");
                NavdataInteractor.Initialise();
                Console.WriteLine("\nDone!\n");
            }
            if (!PerformanceDataService.initialisationStarted)
            {
                Console.Write("Initialising Performance Data Service, please wait...");
                await PerformanceDataService.Initialise();
                Console.WriteLine("\nDone!\n");
            }
            Console.Clear();

            Console.Write("Welcome to the Flight Route Planner!\nEnter departure airport ICAO code: ");
            string departureInput = Console.ReadLine().ToUpper();
            Console.Write("Enter arrival airport ICAO code: ");
            string arrivalInput = Console.ReadLine().ToUpper();

            Console.Write("Enter aircraft type ICAO code: ");
            string acftTypeInput = Console.ReadLine().ToUpper();

            if (departureInput == arrivalInput)
            {
                throw new InvalidRouteInputException();
            }

            if (!AircraftPerformanceAnalyser.SupportedAircraftTypes.Contains(acftTypeInput))
            {
                throw new InvalidAircraftTypeInputException();
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
                route.Aircraft = await Aircraft.CreateAsync(acftTypeInput);

                route = await AircraftPerformanceAnalyser.AddVerticalProfileToRoute(route);

                Console.WriteLine("!bp");

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

        private static async Task StartProgram()
        {
            try
            {
                await RunProgram();
            }
            catch (InvalidRouteInputException)
            {
                Console.WriteLine("\n\nInvalid input.\nOnly enter different valid ICAO airport codes.\nPress any key to restart...");
                Console.ReadKey();
                await StartProgram();
            }
            catch (InvalidAircraftTypeInputException)
            {
                Console.WriteLine("\n\nInvalid input.\nOnly enter valid, supported ICAO aircraft types.");
                Console.WriteLine("Supported aircraft types are:\n");

                Console.WriteLine(string.Join(", ", AircraftPerformanceAnalyser.SupportedAircraftTypes.ToArray()));
                Console.WriteLine("\nPress any key to restart...");
                Console.ReadKey();
                await StartProgram();
            }
        }

        public static async Task Main()
        {
            await StartProgram();

            Console.WriteLine("\n\nPress any key to exit.");
            Console.ReadKey();
        }
    }
}