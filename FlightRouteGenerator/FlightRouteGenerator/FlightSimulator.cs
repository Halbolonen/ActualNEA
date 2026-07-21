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
            FinalApproach
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

        public static async Task<int> GetFlightFuelConsumption(Route route)
        {
            const double dt = 1;
            // seconds
            const double MpS_TO_FpM = 196.85;
            double fuelConsumed;
            // in kilograms
            int verticalSpeed;
            // in meters per second
            int cas;
            // Calibrated AirSpeed, in metres per second
            double mach;
            // mach number
            int tas;
            // True AirSpeed, in metres per second
            int altitude = route.DepartureAirport.altitude;
            Console.WriteLine(altitude);
            // aircraft altitude, in metres
            string serialisedAircraftInfo = JsonSerializer.Serialize(
                new PDS_AircraftRequest
                {
                    aircraft_type = route.Aircraft.ICAOIdent
                }
            );

            int blockFuelEstimate = route.Aircraft.MaxFuelCapacity / 2;


            FlightPhase phaseOfFlight = FlightPhase.Takeoff;
            verticalSpeed = (int)(MpS_TO_FpM * double.Parse(
                await PerformanceDataService.GetResponse("get_initclimb_vs", HttpMethod.Post, serialisedAircraftInfo
                ))
            );
            Console.WriteLine(PerformanceDataService.isInitialised);
            cas = (int)double.Parse(
                await PerformanceDataService.GetResponse("get_climb_init_vcas", HttpMethod.Post, serialisedAircraftInfo
                )
            );
            return 0;
        }
    }
}
