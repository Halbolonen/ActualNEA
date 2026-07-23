using System.Text.Json;

namespace FlightRouteGenerator
{
    internal static class AircraftPerformanceAnalyser
    {
        public static HashSet<string> SupportedAircraftTypes { get; private set; }

        public static async Task<Route> AddVerticalProfileToRoute(Route route)
        {
            const int BAGS_AND_CARGO_PER_PAX = 25;
            // in kilograms
            const int PAX_AND_CARRY_ON_MASS = 80;
            // in kilograms
            const int PAYLOAD_PER_PAX = BAGS_AND_CARGO_PER_PAX + PAX_AND_CARRY_ON_MASS;
            const double KTS_TO_MpS = 0.514444;
            // knots to m/s conversion constant
            const double TAXI_SPEED = 30 * KTS_TO_MpS;
            // in m/s
            const int TAXI_TIME = 15;
            // in minutes
            const int RESERVE_TIME = 30;
            // in minutes
            const double M_TO_FT = 3.28084;
            const double FT_TO_M = 0.3048;
            Random random = new Random();

            string serialisedRequest = JsonSerializer.Serialize(
                new PDS_AircraftRequest
                {
                    aircraft_type = route.Aircraft.ICAOIdent
                });

            int maxFuelCapacity = route.Aircraft.MaxFuelCapacity;
            // max fuel capacity in kilograms

            bool oddFlightLevelRequired = Navigator.GetBearingBetweenGeoCoordinates(route.DepartureAirport.laty, route.DepartureAirport.lonx,
                route.ArrivalAirport.laty, route.ArrivalAirport.lonx) > 180;

            int operationalFlightLevelWithoutTrailingZero = (int)Math.Round(route.Aircraft.CruiseAltitudeLimits.StandardOperations * M_TO_FT / 1000);
            int aircraftCeilingFlightLevelWithoutTrailingZero = (int)Math.Round(route.Aircraft.CruiseAltitudeLimits.Ceiling * M_TO_FT / 1000);
            bool operationalFlightLevelIsOdd = operationalFlightLevelWithoutTrailingZero % 2 > 0;

            void AssignAppropriateFlightLevel()
            {
                if (aircraftCeilingFlightLevelWithoutTrailingZero == operationalFlightLevelWithoutTrailingZero)
                {
                    route.CruiseAltitude = (int)Math.Round((operationalFlightLevelWithoutTrailingZero - 1) * 1000 * FT_TO_M);
                }
                else
                {
                    route.CruiseAltitude = (int)Math.Round((operationalFlightLevelWithoutTrailingZero + 1) * 1000 * FT_TO_M);
                }
            }

            void AssignOperationalFlightLevel()
            {
                route.CruiseAltitude = (int)Math.Round((operationalFlightLevelWithoutTrailingZero) * 1000 * FT_TO_M);
            }

            if (operationalFlightLevelIsOdd && !oddFlightLevelRequired || (!operationalFlightLevelIsOdd && oddFlightLevelRequired))
            {
                AssignAppropriateFlightLevel();
            }
            else
            {
                AssignOperationalFlightLevel();
            }

            route.Loadsheet = new LoadsheetInfo();

            // firstly finding trip fuel with plane loaded to MTOW - MFC to see how much fuel is maximally required.
            // we can then select a load factor under MTOW - block fuel, and optimise fuel again for the load factor.

            int MZFW = route.Aircraft.MTOW - route.Aircraft.MaxFuelCapacity;
            int paxUnderMZFW = Math.Min(MZFW / PAYLOAD_PER_PAX, route.Aircraft.PassengerLoadLimits.high);
            int payloadUnderMZFW = paxUnderMZFW * PAYLOAD_PER_PAX;

            route.Loadsheet.BagsAndCargo = paxUnderMZFW * BAGS_AND_CARGO_PER_PAX;
            route.Loadsheet.Payload = payloadUnderMZFW;
            route.Loadsheet.ZFW = route.Aircraft.OEW + route.Loadsheet.Payload;

            double tripFuel = await FlightSimulator.GetFlightFuelConsumption(route);

            int lowPaxLimit = Math.Max(1, route.Aircraft.PassengerLoadLimits.low / 4);

            route.Loadsheet.Pax = random.Next(lowPaxLimit, paxUnderMZFW + 1);
            route.Loadsheet.BagsAndCargo = route.Loadsheet.Pax * BAGS_AND_CARGO_PER_PAX;
            route.Loadsheet.Payload = route.Loadsheet.Pax * PAX_AND_CARRY_ON_MASS + route.Loadsheet.BagsAndCargo;
            route.Loadsheet.ZFW = route.Aircraft.OEW + route.Loadsheet.Payload;

            tripFuel = await FlightSimulator.GetFlightFuelConsumption(route);

            if (tripFuel > route.Aircraft.MaxFuelCapacity)
            {
                throw new InsufficientAircraftRangeException();
            }

            PDS_TaxiOrReserveFuelParameters taxiParams = new PDS_TaxiOrReserveFuelParameters
            {
                Mass = (int)Math.Round(route.Loadsheet.ZFW + tripFuel),
                TAS = TAXI_SPEED,
                Altitude = route.DepartureAirport.altitude,
                Time = TAXI_TIME,
                AircraftType = route.Aircraft.ICAOIdent
            };

            double taxiFuel = Math.Round(await FlightSimulator.GetTaxiOrReserveFuel(taxiParams));

            // https://skybrary.aero/articles/fuel-flight-planning-definitions
            PDS_TaxiOrReserveFuelParameters reserveParams = new PDS_TaxiOrReserveFuelParameters
            {
                Mass = (int)Math.Round(route.Loadsheet.ZFW),
                TAS = double.Parse(
                    await PerformanceDataService.GetCalculation("cas_to_tas", HttpMethod.Post,
                    JsonSerializer.Serialize(new PDS_AltitudeCAS
                    {
                        Altitude = 1500 * FT_TO_M + route.ArrivalAirport.altitude,
                        CAS = route.Aircraft.FinalApproachCAS
                    }))),
                Altitude = (int)Math.Round(1500 * FT_TO_M + route.ArrivalAirport.altitude),
                Time = RESERVE_TIME,
                AircraftType = route.Aircraft.ICAOIdent
            };

            double reserveFuel = Math.Round(await FlightSimulator.GetTaxiOrReserveFuel(reserveParams));

            route.Loadsheet.BlockFuel = tripFuel + taxiFuel + reserveFuel;
            
            if (route.Loadsheet.BlockFuel > route.Aircraft.MaxFuelCapacity)
            {
                throw new InsufficientAircraftRangeException();
            }

            route.Loadsheet.TOW = route.Loadsheet.ZFW + route.Loadsheet.BlockFuel;
            route.Loadsheet.LAW = route.Loadsheet.TOW - tripFuel - taxiFuel;

            return route;
        }

        static AircraftPerformanceAnalyser()
        {
            SupportedAircraftTypes = new HashSet<string> { "A19N", "A20N", "A21N", "A318", "A319", "A320", "A321", "A332", "A333", "A343",
                "A359", "A388", "B37M", "B38M", "B39M", "B3XM", "B734", "B737", "B738", "B739",
                "B744", "B748", "B752", "B763", "B772", "B773", "B77W", "B788", "B789", "C550",
                "E145", "E170", "E190", "E195", "E75L", "GLF6" };
        }
    }
}
