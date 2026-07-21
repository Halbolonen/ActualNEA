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

        private static int ComputeGrossMass(double fuelMass, Route route)
        {
            int fuelMassInt = (int)Math.Round(Math.Clamp(fuelMass, 0, route.Aircraft.MaxFuelCapacity));
            return fuelMassInt + route.Loadsheet.ZFW;
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

        public static async Task<int> GetFlightFuelConsumption(Route route)
        {
            const double M_TO_NMI = 0.000539957;
            const int dt = 1;
            // seconds
            int cas;
            // Calibrated AirSpeed, in metres/second.
            double mach;
            // mach number
            double descentMach;
            // mach number used in descent
            double distanceTravelled = 0;
            // in nmi
            double descentTrackLength = 0;
            // in nmi
            double climbAndCruiseTrackLength;
            // in nmi
            int blockFuelEstimate = route.Aircraft.MaxFuelCapacity / 2;
            double remainingFuel = blockFuelEstimate;
            Console.WriteLine($"remaining fuel before flight: {remainingFuel} kg");

            FlightPhase phaseOfFlight = FlightPhase.Climb;
            cas = route.Aircraft.InitialClimbCAS;

            PDS_FuelFlowParameters ffParams = new PDS_FuelFlowParameters
            {
                alt = route.DepartureAirport.altitude,
                vs = route.Aircraft.InitialClimbVS,
                mass = ComputeGrossMass((int)Math.Round(Math.Clamp(remainingFuel, 0, route.Aircraft.MaxFuelCapacity)), route),
                aircraft_type = route.Aircraft.ICAOIdent
            };

            async Task RunSimulationTick()
            {
                ffParams.tas = (int)Math.Round(CAStoTAS(ffParams.alt, cas));
                double flow = await GetFuelFlow(ffParams);

                remainingFuel -= flow;
                //Console.WriteLine($"{flow} - [{phaseOfFlight}]");
                ffParams.alt += ffParams.vs * dt;
                ffParams.mass = ComputeGrossMass(remainingFuel, route);

                double angleOfClimb = Math.Asin(ffParams.vs / ffParams.tas);

                switch (phaseOfFlight)
                {
                    case FlightPhase.Climb:
                    case FlightPhase.Cruise:
                        distanceTravelled += ffParams.tas * dt * Math.Cos(angleOfClimb) * M_TO_NMI;
                        break;
                    case FlightPhase.Descent:
                    case FlightPhase.FinalApproach:
                        descentTrackLength += ffParams.tas * dt * Math.Cos(angleOfClimb) * M_TO_NMI;
                        break;
                    default:
                        throw new InvalidOperationException();
                }

            }

            // initial vertical speed & cas

            while (ffParams.alt < route.Aircraft.ClimbXOVerAltConstantCAS)
            {
                await RunSimulationTick();
            }

            // entering constant cas climb
            ffParams.vs = route.Aircraft.ConstantCASClimbVS;
            cas = route.Aircraft.ConstantCASClimbCAS;

            while (ffParams.alt < route.Aircraft.ClimbXOverAltConstantMach)
            {
                await RunSimulationTick();
            }

            // entering constant mach climb
            ffParams.vs = route.Aircraft.ConstantMachClimbVS;
            mach = route.Aircraft.ConstantMachClimbMach;

            while (ffParams.alt < route.CruiseAltitude)
            {
                await RunSimulationTick();
            }

            // simulating descent before simulating cruise
            // entering constant mach stage of descent
            phaseOfFlight = FlightPhase.Descent;
            mach = route.Aircraft.DescentConstMach;
            ffParams.vs = route.Aircraft.DescentConstMachVS;
            while (ffParams.alt > route.Aircraft.DescentXOverAltConstMach)
            {
                await RunSimulationTick();
            }

            // entering constant CAS stage of descent
            cas = route.Aircraft.DescentConstCAS;
            ffParams.vs = route.Aircraft.DescentConstCASVS;
            while (ffParams.alt > route.Aircraft.DescentXOverAltConstCAS)
            {
                await RunSimulationTick();
            }

            // entering final approach stage of descent
            phaseOfFlight = FlightPhase.FinalApproach;
            cas = route.Aircraft.FinalApproachCAS;
            ffParams.vs = route.Aircraft.FinalApproachVS;
            while (ffParams.alt > route.ArrivalAirport.altitude)
            {
                await RunSimulationTick();
            }

            climbAndCruiseTrackLength = route.TotalDistance - descentTrackLength;

            // entering cruise
            ffParams.vs = 0;
            mach = route.Aircraft.CruiseMach;
            phaseOfFlight = FlightPhase.Cruise;

            while (distanceTravelled < climbAndCruiseTrackLength)
            {
                await RunSimulationTick();
            }

            remainingFuel = Math.Round(remainingFuel);

            Console.WriteLine($"remaining fuel after flight: {remainingFuel} kg");

            return 0;
        }
    }
}
