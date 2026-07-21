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
        public string SerialisedAircraftInfo { get; set; }
        // json-serialised object for use with the Performance Data Service
        public int InitialClimbVS { get; set; }
        // initial vertical speed in the climb in m/s
        public int ConstantCASClimbVS { get; set;}
        // vertical speed in stage of climb with constant
        // Calibrated AirSpeed, in m/s
        public int ConstantMachClimbVS { get; set; }
        // vertical speed in stage of climb with constant
        // mach number, in m/s
        public int ClimbXOVerAltConstantCAS { get; set; }
        // altitude in the climb when aircraft assumes constant CAS in m
        public int ClimbXOverAltConstantMach { get; set; }
        // altitude in the climb when aircraft assumes constant mach number in m
        public int InitialClimbCAS { get; set; }
        // initial Calibrated AirSpeed in m/s
        public int ConstantCASClimbCAS { get; set; }
        // Calibrated AirSpeed in the point in the climb at which
        // it is constant, in m/s
        public double ConstantMachClimbMach { get; set; }
        // mach number at the point in the climb at which
        // it is constant.

        public Aircraft(string inICAOIdent)
        {
            ICAOIdent = inICAOIdent;
        }

        private async Task<int> GetIntDatapointFromPDS(string path)
        {
            int datapoint = (int)
                Math.Round(
                    double.Parse(
                        await PerformanceDataService.GetResponse(path, HttpMethod.Post, SerialisedAircraftInfo
                        )
                    )
                );

            return datapoint;
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

            aircraft.SerialisedAircraftInfo = JsonSerializer.Serialize(aircraftRequest);

            aircraft.PassengerLoadLimits = JsonSerializer.Deserialize<PassengerLoadRange>(
                await PerformanceDataService.GetResponse("get_aircraft_passenger_load_range", HttpMethod.Post, aircraft.SerialisedAircraftInfo)
                );
            aircraft.OEW = await aircraft.GetIntDatapointFromPDS("get_aircraft_oew");
            aircraft.MaxFuelCapacity = await aircraft.GetIntDatapointFromPDS("get_aircraft_fuel_capacity");
            aircraft.InitialClimbVS = await aircraft.GetIntDatapointFromPDS("get_initclimb_vs");
            aircraft.ConstantCASClimbVS = await aircraft.GetIntDatapointFromPDS("get_climb_vs_const_cas");
            aircraft.ClimbXOVerAltConstantCAS = 1000 * await aircraft.GetIntDatapointFromPDS("get_climb_concas_xover_alt");
            aircraft.ClimbXOverAltConstantMach = 1000 * await aircraft.GetIntDatapointFromPDS("get_climb_conmach_xover_alt");
            
            return aircraft;
        }
    }
}
