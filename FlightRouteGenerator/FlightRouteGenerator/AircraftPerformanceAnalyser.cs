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
            Random random = new Random();

            string serialisedRequest = JsonSerializer.Serialize(
                new PDS_AircraftRequest
                {
                    aircraft_type = route.Aircraft.ICAOIdent
                });

            int maxFuelCapacity = route.Aircraft.MaxFuelCapacity;
            // max fuel capacity in kilograms

            route.Loadsheet = new LoadsheetInfo
            {
                Pax = random.Next(
                route.Aircraft.PassengerLoadLimits.low,
                route.Aircraft.PassengerLoadLimits.max + 1
                ),
            };

            route.Loadsheet.BagsAndCargo = route.Loadsheet.Pax * BAGS_AND_CARGO_PER_PAX;
            route.Loadsheet.Payload = route.Loadsheet.Pax * PAX_AND_CARRY_ON_MASS + route.Loadsheet.BagsAndCargo;
            route.Loadsheet.ZFW = route.Aircraft.OEW + route.Loadsheet.Payload;

            double tripFuel = await FlightSimulator.GetFlightFuelConsumption(route);
            route.Loadsheet.BlockFuel = tripFuel; // FIXME
            route.Loadsheet.TOW = route.Loadsheet.ZFW + route.Loadsheet.BlockFuel;
            route.Loadsheet.LAW = route.Loadsheet.TOW - tripFuel;

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
