using System;
using System.Text.Json;
using System.Threading;

namespace FlightRouteGenerator
{
    class Program
    {
        private static async Task RestartProgram()
        {
            Console.WriteLine("\nPress any key to restart...");
            Console.ReadKey();
            Console.Write("Are you sure you want to restart? Y/N: ");
            Console.CursorVisible = true;
            if (Console.ReadLine().ToUpper() == "Y")
            {
                Console.CursorVisible = false;
                await StartProgram();
            }
            else
            {
                Console.CursorVisible = false;
                RestartProgram();
            }
        }

        private static async Task CreateNewFlightPlan()
        {
            Console.WriteLine("\nCREATING A NEW FLIGHT PLAN");
            Console.Write("\nEnter departure airport ICAO code: ");
            Console.CursorVisible = true;
            string departureInput = Console.ReadLine().ToUpper();
            Console.Write("Enter arrival airport ICAO code: ");
            string arrivalInput = Console.ReadLine().ToUpper();

            Console.Write("Enter aircraft type ICAO code: ");
            string acftTypeInput = Console.ReadLine().ToUpper();
            Console.CursorVisible = false;

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

            if (!AircraftPerformanceAnalyser.SupportedAircraftTypes.Contains(acftTypeInput))
            {
                throw new InvalidAircraftTypeInputException();
            }


            AStarSearch aStar = new AStarSearch();
            Route route;

            try
            {
                Console.Write("\nFinding a route...");
                route = aStar.GetRouteBetweenAirports(departureAirport, arrivalAirport);
                route.Aircraft = await Aircraft.CreateAsync(acftTypeInput);
                Console.WriteLine("\nDone!\n");
                Console.Write("Evaluating aircraft performance...");


                try
                {
                    route = await AircraftPerformanceAnalyser.AddVerticalProfileToRoute(route);
                    Console.WriteLine("\nDone!\n");
                }
                catch (InsufficientAircraftRangeException)
                {
                    throw;
                }

                Console.Clear();
                Console.WriteLine("Use the menu to select the formats you want your flight plan to be outputted in.\n");

                List<string> outputOptions = new List<string> { "Console", "PDF File", "X-Plane route file (.fms)", "Microsoft Flight Simulator route file (.pln)" };
                HashSet<int> choices = MultipleChoiceMenu.GetMultiSelectChoice(outputOptions);
                List<string> outputSuccessMessages = new List<string>();

                foreach (int choice in choices)
                {
                    switch (choice)
                    {
                        case 0:
                            PlanOutputManager.OutputRouteToConsole(route);
                            break;

                        case 1:
                            outputSuccessMessages.Add(PlanOutputManager.OutputRouteToPDFFile(route));
                            break;

                        case 2:
                            outputSuccessMessages.Add(PlanOutputManager.OutputRouteToFMSFile(route));
                            break;

                        case 3:
                            outputSuccessMessages.Add(PlanOutputManager.OutputRouteToPLNFile(route));
                            break;
                    }
                }

                foreach (string msg in outputSuccessMessages)
                {
                    Console.WriteLine(msg);
                }

                await RestartProgram();
            }
            catch (RouteDiscontinuityException)
            {
                Console.WriteLine($"\nUnfortunately, no route could be found between {departureAirport.ident} and {arrivalAirport.ident}.");
                await RestartProgram();
            }
            catch (InsufficientAircraftRangeException)
            {
                Console.WriteLine($"\n\nUnfortunately, the maximum range of your selected aircraft, {acftTypeInput}, is too low for your selected flight.\nTry again for an aircraft with a longer range, or try a shorter flight.");
                await RestartProgram();
            }
        }

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

            Console.WriteLine("Welcome to the Flight Plan Generator!\nChoose what you would like to do:\n");
            List<string> mainMenuChoices = new List<string> {"Create a new flight plan", "Exit the program"};
            int choice = MultipleChoiceMenu.GetSingleSelectChoice(mainMenuChoices);

            switch (choice)
            {
                case 0:
                    await CreateNewFlightPlan();
                    break;
                case 1:
                    Console.Write("\nStopping services...");
                    if (PerformanceDataService.isInitialised)
                    {
                        PerformanceDataService.KillService();
                    }
                    Console.WriteLine("\nDone!");
                    break;
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
                Console.WriteLine("\n\nInvalid input.\nOnly enter different valid ICAO airport codes.");
                await RestartProgram();
            }
            catch (InvalidAircraftTypeInputException)
            {
                Console.WriteLine("\n\nInvalid input.\nOnly enter valid, supported ICAO aircraft types.");
                Console.WriteLine("Supported aircraft types are:\n");

                Console.WriteLine(string.Join(", ", AircraftPerformanceAnalyser.SupportedAircraftTypes.ToArray()));
                await RestartProgram();
            }
        }

        public static async Task Main()
        {
            Console.CursorVisible = false;
            await StartProgram();

            Console.WriteLine("\n\nPress any key to exit.");
            Console.ReadKey();
            Console.CursorVisible = true;
        }
    }
}