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

            int maxFuelCapacity = int.Parse(await PerformanceDataService.GetResponse(
                "get_aircraft_fuel_capacity", HttpMethod.Post, serialisedRequest
                ));
            // max fuel capacity in kilograms

            int blockFuelEstimate = maxFuelCapacity / 2;

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

            int consumedFuel = await FlightSimulator.GetFlightFuelConsumption(
                blockFuelEstimate, route.Loadsheet.ZFW, route.Aircraft.ICAOIdent
                );

            throw new NotImplementedException();
        }

        static AircraftPerformanceAnalyser()
        {
            SupportedAircraftTypes = new HashSet<string> { "A320" };
        }
    }
}
