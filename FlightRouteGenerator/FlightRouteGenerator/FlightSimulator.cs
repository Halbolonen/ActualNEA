using System.Runtime.CompilerServices;
using System.Text.Json;

namespace FlightRouteGenerator
{
    internal static class FlightSimulator
    {
        private enum FlightPhase
        {
            Takeoff,
            Climb,
            Cruise,
            Descent,
            FinalApproach,
            DescentTrackLengthComputation
        }

        public static double CAStoTAS(double altitude, double cas)
        {
            // https://ntrs.nasa.gov/api/citations/19770009539/downloads/19770009539.pdf

            // https://www.grc.nasa.gov/www/k-12/VirtualAero/BottleRocket/airplane/sound.html

            // https://www.grc.nasa.gov/www/k-12/airplane/atmosmet.html

            const double t_0 = 288.15;
            // ISA standard temperature at sea level, in kelvin.
            const double p_0 = 101.325;
            // ISA standard pressure at sea level, in kilopascals.
            const double R = 287;
            // Gas constant for air, in m^2/s^2/K.
            const double gamma = 1.40;
            // specific heat ratio for calorically perfect air.
            const double a_0 = 340.294;
            // speed of sound at sea level, m/s.
            const double ISA_LAPSE_RATE = 6.5;
            // loss of air temperature in centigrade per km.

            double t = t_0 - (altitude / 1000) * ISA_LAPSE_RATE;

            double localSpeedOfSound = Math.Sqrt(
                gamma * R * t
            );
            // https://www.grc.nasa.gov/www/k-12/VirtualAero/BottleRocket/airplane/sound.html

            double P = p_0 * Math.Pow(t / t_0, 5.256);
            // TODO: label magic number -5.256
            // static pressure at altitude

            double qc = p_0 * (Math.Pow(1 + 0.2 * Math.Pow((cas / a_0), 2), 3.5) - 1);
            // subsonic impact pressure, mach

            double machNumber = Math.Sqrt(
                5 * (Math.Pow((qc / P) + 1, 1 / 3.5) - 1)
            );
            // subsonic mach number
            // https://www.aviationhunt.com/airspeed-conversion-calculator/

            return machNumber * localSpeedOfSound;
        }

        public static double MachToTAS(double altitude, double machNumber)
        {
            const double gamma = 1.40;
            // specific heat ratio for calorically perfect air.
            const double R = 287;
            // Gas constant for air, in m^2/s^2/K.
            const double t_0 = 288.15;
            // ISA standard temperature at sea level, in kelvin.
            const double ISA_LAPSE_RATE = 6.5;
            // loss of air temperature in centigrade per km.
            double t = t_0 - (altitude / 1000) * ISA_LAPSE_RATE;

            double localSpeedOfSound = Math.Sqrt(
                gamma * R * t
            );
            // https://www.grc.nasa.gov/www/k-12/VirtualAero/BottleRocket/airplane/sound.html

            return machNumber * localSpeedOfSound;
        }

        private static double ComputeGrossMass(double fuelMass, Route route)
        {
            return Math.Clamp(fuelMass, 0, route.Aircraft.MaxFuelCapacity) + route.Loadsheet.ZFW;
        }

        private static async Task<double> GetFuelFlow(PDS_FuelFlowParameters ffParams)
        {
            string serialisedFFParams = JsonSerializer.Serialize(ffParams);
            double fuelFlow;
            fuelFlow = double.Parse(
                    await PerformanceDataService.GetResponse("get_fuelflow", HttpMethod.Post, serialisedFFParams
                    )
                );


            return fuelFlow;
        }

        public static async Task<double> GetFlightFuelConsumption(Route route)
        {
            double burnedFuel = route.Aircraft.MaxFuelCapacity / 2;
            const double M_TO_FT = 3.28084;

            double trackDistance = 0;
            List<PDS_WaypointTrackDistance> WaypointIDToTrackDistance = new List<PDS_WaypointTrackDistance>();

            foreach (RouteLeg leg in route.Legs)
            {
                trackDistance += leg.Length;
                WaypointIDToTrackDistance.Add(
                    new PDS_WaypointTrackDistance
                    {
                        WaypointID = leg.Waypoint.WaypointID,
                        TrackDistance = trackDistance
                    }
                    );
            }

            PDS_FlightRequest flightRequest = new PDS_FlightRequest
            {
                DepartureAirportAltitude = route.DepartureAirport.altitude,
                ArrivalAirportAltitude = route.ArrivalAirport.altitude,
                AircraftRequest = new PDS_AircraftRequest
                {
                    aircraft_type = route.Aircraft.ICAOIdent
                },
                ZFW = route.Loadsheet.ZFW,
                CruiseAltitude = route.CruiseAltitude,
                RouteTotalDistance = route.TotalDistance,
                TripFuel = burnedFuel,
                WaypointIDToTrackDistance = WaypointIDToTrackDistance
            };

            string serialisedRequest = JsonSerializer.Serialize(flightRequest);
            bool fuelOptimal = false;
            PDS_SimulatorResult simResult = new PDS_SimulatorResult();

            while (!fuelOptimal)
            {
                string flightData = await PerformanceDataService.GetCalculation("simulate_flight", HttpMethod.Post, serialisedRequest);
                simResult = JsonSerializer.Deserialize<PDS_SimulatorResult>(flightData
                       );

                burnedFuel = simResult.TripFuel;

                if (flightRequest.TripFuel - burnedFuel < 5)
                {
                    fuelOptimal = true;
                }
                else
                {
                    flightRequest.TripFuel = burnedFuel;
                    serialisedRequest = JsonSerializer.Serialize(flightRequest);
                }
            }

            foreach (RouteLeg leg in route.Legs)
            {
                PDS_WaypointInfo wpInfo = simResult.WaypointIDToInfo[leg.Waypoint.WaypointID];
                leg.Waypoint.Altitude = (int)(M_TO_FT * wpInfo.Altitude);
                leg.Waypoint.TAS = wpInfo.TAS;
            }

            return burnedFuel;
        }
    }
}
