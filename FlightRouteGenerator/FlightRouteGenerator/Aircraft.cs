using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace FlightRouteGenerator
{
    internal class Aircraft
    {
        public string ICAOIdent { get; set; }
        public int TakeoffWeight { get; set; }
        // in kilograms
        public int CruiseFlightLevel { get; set; }
        // in hundreds of feet
        public double CruiseMach { get; set; }
        // in mach
        public int ClimbCAS { get; set; }
        // Climb Calibrated AirSpeed, in knots
        public int InitVS { get; set; }
        // Initial Vertical Speed, in feet per minute
        public int PreConstantCAS_VS { get; set; }
        // vertical speed before constant Calibrated
        // AirSpeed, in feet per minute
        public int ConstantCAS_VS { get; set; }
        // vertical speed during constant Calibrated
        // AirSpeed, in feet per minute.
        public int ConstantMach_VS { get; set; }
        // vertical speed during constant Mach, in
        // feet per minute.
        public struct PassengerLoadRange
        {
            public int high { get; set; }
            public int low { get; set; }
            public int max { get; set; }
        }
        public PassengerLoadRange PassengerLoadLimits { get; set; }
        public int OEW { get; set; }
        // Operational Empty Weight, in kilograms
        public int MaxFuelCapacity { get; set; }
        // in kilograms

        private static async Task<string> InteractWithPDS(string path, HttpMethod httpMethod, string serialisedRequest)
        {
            return await PerformanceDataService.GetResponse(path, httpMethod, serialisedRequest);
        }

        public Aircraft(string inICAOIdent)
        {
            ICAOIdent = inICAOIdent;
        }

        public static async Task<Aircraft> CreateAsync(string inICAOIdent)
        {
            const double MpS_TO_FpM = 196.85;
            // metres per second to feet per minute

            const double MpS_TO_KTS = 1.94384;
            // metres per second to knots

            Aircraft aircraft = new Aircraft(inICAOIdent);

            PDS_AircraftRequest aircraftRequest = new PDS_AircraftRequest
            {
                aircraft_type = aircraft.ICAOIdent
            };

            string serialisedAircraftRequest = JsonSerializer.Serialize(aircraftRequest);

            aircraft.PassengerLoadLimits = JsonSerializer.Deserialize<PassengerLoadRange>(
                await InteractWithPDS("get_aircraft_passenger_load_range", HttpMethod.Post, serialisedAircraftRequest)
                );
            aircraft.OEW = int.Parse(
                await InteractWithPDS("get_aircraft_oew", HttpMethod.Post, serialisedAircraftRequest)
                );

            aircraft.MaxFuelCapacity = int.Parse(
                await InteractWithPDS("get_aircraft_fuel_capacity", HttpMethod.Post, serialisedAircraftRequest)
                );

            return aircraft;
        }
    }
}
