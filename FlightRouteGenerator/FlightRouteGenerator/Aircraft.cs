using System.Text.Json;

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

        private async Task<string> InteractWithPDS(string path, HttpMethod httpMethod, string serialisedRequest)
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

            aircraft.ClimbCAS = (int)Math.Round(
                MpS_TO_FpM * double.Parse(
                    await aircraft.InteractWithPDS(
                        "get_wrap_climb_const_vcas_mean", HttpMethod.Post, serialisedAircraftRequest)
                    )
                );

            aircraft.InitVS = (int)Math.Round(
                MpS_TO_KTS * double.Parse(
                    await aircraft.InteractWithPDS(
                        "get_wrap_initclimb_vs_mean", HttpMethod.Post, serialisedAircraftRequest)
                    )
                );

            aircraft.PreConstantCAS_VS = (int)Math.Round(
                MpS_TO_FpM * double.Parse(
                    await aircraft.InteractWithPDS(
                        "get_wrap_climb_vs_pre_concas_mean", HttpMethod.Post, serialisedAircraftRequest)
                    )
                );

            aircraft.ConstantCAS_VS = (int)Math.Round(
                MpS_TO_FpM * double.Parse(
                    await aircraft.InteractWithPDS(
                        "get_wrap_climb_vs_concas_mean", HttpMethod.Post, serialisedAircraftRequest)
                    )
                );

            aircraft.ConstantMach_VS = (int)Math.Round(
                MpS_TO_FpM * double.Parse(
                    await aircraft.InteractWithPDS(
                        "get_wrap_climb_vs_concas_mean", HttpMethod.Post, serialisedAircraftRequest)
                    )
                );

            return aircraft;
        }
    }
}
