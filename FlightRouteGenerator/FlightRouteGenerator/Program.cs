using System;
using System.Text.Json;
using System.Threading;

namespace FlightRouteGenerator
{
    class Program
    {

        private static async Task CreateNewFlightPlan()
        {
            Console.WriteLine("\nCREATING A NEW FLIGHT PLAN");
            Console.WriteLine("The ICAO codes of the aircraft types that the program supports are as follows:\n");
            Console.WriteLine(string.Join(", ", AircraftPerformanceAnalyser.SupportedAircraftTypes.ToArray()));
            Console.CursorVisible = true;
            Console.Write("\nEnter aircraft type ICAO code: ");
            string acftTypeInput = Console.ReadLine().ToUpper();
            Console.Write("\nEnter departure airport ICAO code: ");
            Console.CursorVisible = true;
            string departureInput = Console.ReadLine().ToUpper();
            Console.Write("Enter arrival airport ICAO code: ");
            string arrivalInput = Console.ReadLine().ToUpper();

            Console.CursorVisible = false;

            AirportRecord departureAirport = new AirportRecord();
            AirportRecord arrivalAirport = new AirportRecord();
            List<UserInputIssue> inputIssues = new List<UserInputIssue>();

            if (departureInput == arrivalInput)
            {
                inputIssues.Add(UserInputIssue.DepArrAirportsAreIdentical);
            }

            try
            {
                departureAirport = NavdataInteractor.FindAirportByIdent(departureInput);
            }
            catch (AirportNotFoundByIdentException)
            {
                inputIssues.Add(UserInputIssue.DepartureAirport);
            }

            try
            {
                arrivalAirport = NavdataInteractor.FindAirportByIdent(arrivalInput);
            }
            catch (AirportNotFoundByIdentException)
            {
                inputIssues.Add(UserInputIssue.ArrivalAirport);
            }

            if (!AircraftPerformanceAnalyser.SupportedAircraftTypes.Contains(acftTypeInput))
            {
                inputIssues.Add(UserInputIssue.AircraftType);
            }

            if (inputIssues.Count > 0)
            {
                Console.WriteLine();
                foreach (UserInputIssue issue in inputIssues)
                {
                    switch (issue)
                    {
                        case UserInputIssue.AircraftType:
                            Console.WriteLine("Invalid aircraft type input. Only enter valid, supported ICAO aircraft types.");
                            break;
                        case UserInputIssue.DepartureAirport:
                            Console.WriteLine("Invalid departure airport input. Only enter valid ICAO airport codes.");
                            break;
                        case UserInputIssue.ArrivalAirport:
                            Console.WriteLine("Invalid arrival airport input. Only enter valid ICAO airport codes.");
                            break;
                        case UserInputIssue.DepArrAirportsAreIdentical:
                            Console.WriteLine("Departure and arrival airports cannot be the same.");
                            break;
                    }
                }
                Console.WriteLine('\n');
                throw new FatalUserInputException();
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
                // no console output option, just give a correctly formatted route in the pdf that you can paste into flight route plotting software/to read.
                List<string> outputOptions = new List<string> {"PDF File", "X-Plane route file (.fms)", "Microsoft Flight Simulator route file (.pln)" };
                HashSet<int> choices = MultipleChoiceMenu.GetMultiSelectChoice(outputOptions);
                List<string> outputSuccessMessages = new List<string>();

                foreach (int choice in choices)
                {
                    switch (choice)
                    {
                        case 0:
                            outputSuccessMessages.Add(PlanOutputManager.OutputRouteToPDFFile(route));
                            break;

                        case 1:
                            outputSuccessMessages.Add(PlanOutputManager.OutputRouteToFMSFile(route));
                            break;

                        case 2:
                            outputSuccessMessages.Add(PlanOutputManager.OutputRouteToPLNFile(route));
                            break;
                    }
                }

                foreach (string msg in outputSuccessMessages)
                {
                    Console.WriteLine(msg);
                }
            }
            catch (RouteDiscontinuityException)
            {
                Console.WriteLine($"\nUnfortunately, no route could be found between {departureAirport.ident} and {arrivalAirport.ident}.");
            }
            catch (InsufficientAircraftRangeException)
            {
                Console.WriteLine($"\n\nUnfortunately, the maximum range of your selected aircraft, {acftTypeInput}, is too low for your selected flight.\nTry again for an aircraft with a longer range, or try a shorter flight.");
            }
        }

        private static async Task InitialiseServices()
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
        }

        public static async Task Main()
        {
            Console.CursorVisible = false;
            await InitialiseServices();
            bool programRunning = true;
            while (programRunning)
            {

                Console.Clear();

                Console.WriteLine("Welcome to the Flight Plan Generator!\nChoose what you would like to do:\n");
                List<string> mainMenuChoices = new List<string> { "Create a new flight plan", "Exit the program" };
                int choice = MultipleChoiceMenu.GetSingleSelectChoice(mainMenuChoices);

                switch (choice)
                {
                    case 0:
                        try
                        {
                            await CreateNewFlightPlan();
                        }
                        catch (FatalUserInputException)
                        {
                            Console.WriteLine("Your invalid inputs mean that the creation of the flight plan must be aborted.");
                        }
                        break;
                    case 1:
                        programRunning = false;
                        break;
                }

                if (!programRunning)
                {
                    break;
                }

                bool commenceRestart = false;

                while (!commenceRestart)
                {
                    Console.WriteLine("\nPress any key to restart...");
                    Console.ReadKey();
                    Console.Write("Are you sure you want to restart? Y/N: ");
                    Console.CursorVisible = true;
                    if (Console.ReadLine().ToUpper() == "Y")
                    {
                        commenceRestart = true;
                    }
                    else
                    {
                        Console.WriteLine();
                    }
                }
            }

            Console.Write("\nStopping services...");
            if (PerformanceDataService.isInitialised)
            {
                PerformanceDataService.KillService();
            }
            Console.WriteLine("\nDone!");

            Console.WriteLine("\n\nPress any key to exit.");
            Console.ReadKey();
            Console.CursorVisible = true;
        }
    }
}