using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace FlightRouteGenerator
{
    internal class Aircraft
    {
        public string ICAOIdent { get; set; }
        public int TakeoffWeight { get; set; }
        // in kilograms
        public int MTOW { get; set; }
        // Maximum TakeOff Weight, in kilograms
        public int MLW { get; set; }
        // Maximum Landing Weight, in kilograms
        public struct PassengerLoadRange
        {
            public int high { get; set; }
            public int low { get; set; }
            public int max { get; set; }
        }
        public PassengerLoadRange PassengerLoadLimits { get; set; }
        public struct CruiseAltitudeRange
        {
            [JsonPropertyName("std_operations")]
            public double StandardOperations { get; set; }
            [JsonPropertyName("ceiling")]
            public double Ceiling { get; set; }
        }
        public CruiseAltitudeRange CruiseAltitudeLimits { get; set; }
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
        // altitude in the climb when aircraft assumes constant CAS in metres
        public int ClimbXOverAltConstantMach { get; set; }
        // altitude in the climb when aircraft assumes constant mach number in metres
        public int InitialClimbCAS { get; set; }
        // initial Calibrated AirSpeed in m/s
        public int ConstantCASClimbCAS { get; set; }
        // Calibrated AirSpeed in the point in the climb at which
        // it is constant, in m/s
        public double ConstantMachClimbMach { get; set; }
        // mach number at the point in the climb at which
        // it is constant.
        public double CruiseMach { get; set; }
        // mach number the aircraft uses in cruise
        public double DescentConstMach { get; set; }
        // mach number the aircraft uses during the phase of
        // descent where it is constant
        public int DescentConstMachVS { get; set; }
        // vertical speed the aircraft uses during the phase
        // of descent where mach number is constant
        public int DescentConstCAS { get; set; }
        // Calibrated AirSpeed the aircraft uses during the
        // phase of descent where CAS is constant
        public int DescentConstCASVS { get; set; }
        // vertical speed the aircraft uses during the
        // phase of descent where CAS is constant
        public int DescentXOverAltConstMach { get; set; }
        // altitude in the descent when aircraft stops assuming
        // constant mach number in m
        public int DescentXOverAltConstCAS { get; set; }
        // altitude in the descent when aircraft stops assuming
        // constant Calibrated AirSpeed in m
        public int FinalApproachCAS { get; set; }
        // Calibrated AirSpeed on final approach in m/s
        public int FinalApproachVS { get; set; }
        // vertical speed on final approach in m/s

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

        private async Task<double> GetDoubleDatapointFromPDS(string path)
        {
            return double.Parse(await PerformanceDataService.GetResponse(path, HttpMethod.Post, SerialisedAircraftInfo));
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
            aircraft.CruiseAltitudeLimits = JsonSerializer.Deserialize<CruiseAltitudeRange>(
                await PerformanceDataService.GetResponse("get_aircraft_cruise_alt_range", HttpMethod.Post, aircraft.SerialisedAircraftInfo)
                );
            aircraft.OEW = await aircraft.GetIntDatapointFromPDS("get_aircraft_oew");
            aircraft.MaxFuelCapacity = await aircraft.GetIntDatapointFromPDS("get_aircraft_fuel_capacity");
            aircraft.InitialClimbVS = await aircraft.GetIntDatapointFromPDS("get_initclimb_vs");
            aircraft.InitialClimbCAS = await aircraft.GetIntDatapointFromPDS("get_climb_init_vcas");
            aircraft.ConstantCASClimbVS = await aircraft.GetIntDatapointFromPDS("get_climb_vs_const_cas");
            aircraft.ConstantCASClimbCAS = await aircraft.GetIntDatapointFromPDS("get_climb_const_vcas");
            aircraft.ConstantMachClimbMach = await aircraft.GetDoubleDatapointFromPDS("get_climb_const_mach");
            aircraft.ConstantMachClimbVS = await aircraft.GetIntDatapointFromPDS("get_climb_vs_const_mach");
            aircraft.ClimbXOVerAltConstantCAS = 1000 * await aircraft.GetIntDatapointFromPDS("get_climb_concas_xover_alt");
            aircraft.ClimbXOverAltConstantMach = 1000 * await aircraft.GetIntDatapointFromPDS("get_climb_conmach_xover_alt");
            aircraft.CruiseMach = await aircraft.GetDoubleDatapointFromPDS("get_cruise_mach");
            aircraft.DescentConstMach = await aircraft.GetDoubleDatapointFromPDS("get_descent_const_mach");
            aircraft.DescentConstMachVS = await aircraft.GetIntDatapointFromPDS("get_descent_vs_const_mach");
            aircraft.DescentConstCAS = await aircraft.GetIntDatapointFromPDS("get_descent_const_vcas");
            aircraft.DescentConstCASVS = await aircraft.GetIntDatapointFromPDS("get_descent_vs_const_cas");
            aircraft.DescentXOverAltConstMach = 1000 * await aircraft.GetIntDatapointFromPDS("get_descent_xover_alt_const_mach");
            aircraft.DescentXOverAltConstCAS = 1000 * await aircraft.GetIntDatapointFromPDS("get_descent_xover_alt_const_cas");
            aircraft.FinalApproachCAS = await aircraft.GetIntDatapointFromPDS("get_finalapp_vcas");
            aircraft.FinalApproachVS = await aircraft.GetIntDatapointFromPDS("get_finalapp_vs");
            aircraft.MTOW = await aircraft.GetIntDatapointFromPDS("get_aircraft_mtow");
            aircraft.MLW = await aircraft.GetIntDatapointFromPDS("get_aircraft_mlw");
            
            
            return aircraft;
        }
    }
}
