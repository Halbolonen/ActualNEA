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
                RouteTotalDistance = route.TotalDistance
            };

            string serialisedRequest = JsonSerializer.Serialize(flightRequest);

            return (int)Math.Round(
                double.Parse(
                    await PerformanceDataService.GetCalculation("get_flight_fuel_consumption", HttpMethod.Post, serialisedRequest)
                    )
                );
        }

        /*public static async Task<int> GetFlightFuelConsumption(Route route)
        {
            const double M_TO_NMI = 0.000539957;
            const int dt = 10;
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
            //int blockFuelEstimate = route.Aircraft.MaxFuelCapacity / 2;
            int blockFuelEstimate = 9050;
            double remainingFuel = blockFuelEstimate;
            PDS_FuelFlowParameters descentFFParams = new PDS_FuelFlowParameters();
            Console.WriteLine($"remaining fuel before flight: {remainingFuel} kg");

            FlightPhase phaseOfFlight = FlightPhase.Climb;
            cas = route.Aircraft.InitialClimbCAS;

            PDS_FuelFlowParameters ffParams = new PDS_FuelFlowParameters
            {
                alt = route.DepartureAirport.altitude,
                vs = route.Aircraft.InitialClimbVS,
                mass = ComputeGrossMass(remainingFuel, route),
                aircraft_type = route.Aircraft.ICAOIdent
            };

            async Task RunSimulationTick()
            {
                double flow;
                double angleOfClimb;
                switch (phaseOfFlight)
                {
                    case FlightPhase.DescentTrackLengthComputation:
                        angleOfClimb = Math.Asin(descentFFParams.vs / descentFFParams.tas);
                        descentTrackLength += descentFFParams.tas * dt * Math.Cos(angleOfClimb) * M_TO_NMI;
                        descentFFParams.alt += descentFFParams.vs * dt;
                        break;
                    default:
                        angleOfClimb = Math.Asin(ffParams.vs / ffParams.tas);
                        flow = await GetFuelFlow(ffParams);

                        remainingFuel -= flow * dt;
                        //Console.WriteLine($"{flow} - [{phaseOfFlight}]");
                        ffParams.alt += ffParams.vs * dt;
                        ffParams.mass = ComputeGrossMass(remainingFuel, route);
                        distanceTravelled += ffParams.tas * dt * Math.Cos(angleOfClimb) * M_TO_NMI;

                        break;
                }


            }

            // initial vertical speed & cas

            while (ffParams.alt < route.Aircraft.ClimbXOVerAltConstantCAS)
            {
                ffParams.tas = (int)Math.Round(CAStoTAS(ffParams.alt, cas));
                await RunSimulationTick();
            }

            // entering constant cas climb
            ffParams.vs = route.Aircraft.ConstantCASClimbVS;
            cas = route.Aircraft.ConstantCASClimbCAS;

            while (ffParams.alt < route.Aircraft.ClimbXOverAltConstantMach)
            {
                ffParams.tas = (int)Math.Round(CAStoTAS(ffParams.alt, cas));
                await RunSimulationTick();
            }

            // entering constant mach climb
            ffParams.vs = route.Aircraft.ConstantMachClimbVS;
            mach = route.Aircraft.ConstantMachClimbMach;

            while (ffParams.alt < route.CruiseAltitude)
            {
                ffParams.tas = (int)Math.Round(MachToTAS(ffParams.alt, mach));
                await RunSimulationTick();
            }

            // getting descent track length before simulating cruise
            phaseOfFlight = FlightPhase.DescentTrackLengthComputation;
            descentFFParams = ffParams;
            // entering constant mach stage of descent
            mach = route.Aircraft.DescentConstMach;
            descentFFParams.vs = route.Aircraft.DescentConstMachVS;
            while (descentFFParams.alt > route.Aircraft.DescentXOverAltConstMach)
            {
                descentFFParams.tas = (int)Math.Round(MachToTAS(descentFFParams.alt, mach));
                await RunSimulationTick();
            }

            // entering constant CAS stage of descent
            cas = route.Aircraft.DescentConstCAS;
            descentFFParams.vs = route.Aircraft.DescentConstCASVS;
            while (descentFFParams.alt > route.Aircraft.DescentXOverAltConstCAS)
            {
                descentFFParams.tas = (int)Math.Round(CAStoTAS(descentFFParams.alt, cas));
                await RunSimulationTick();
            }

            // entering final approach stage of descent
            cas = route.Aircraft.FinalApproachCAS;
            descentFFParams.vs = route.Aircraft.FinalApproachVS;
            while (descentFFParams.alt > route.ArrivalAirport.altitude)
            {
                ffParams.tas = (int)Math.Round(CAStoTAS(descentFFParams.alt, cas));
                await RunSimulationTick();
            }

            climbAndCruiseTrackLength = route.TotalDistance - descentTrackLength;

            // entering cruise
            int cruiseCounter = 0;
            ffParams.vs = 0;
            mach = route.Aircraft.CruiseMach;
            phaseOfFlight = FlightPhase.Cruise;
            ffParams.alt = route.CruiseAltitude;
            ffParams.tas = (int)Math.Round(MachToTAS(ffParams.alt, mach));

            while (distanceTravelled < climbAndCruiseTrackLength)
            {
                cruiseCounter += dt;
                await RunSimulationTick();
            }

            // simulating descent stages for real this time
            // entering constant mach stage of descent
            phaseOfFlight = FlightPhase.Descent;
            mach = route.Aircraft.DescentConstMach;
            ffParams.vs = route.Aircraft.DescentConstMachVS;
            while (ffParams.alt > route.Aircraft.DescentXOverAltConstMach)
            {
                ffParams.tas = (int)Math.Round(MachToTAS(ffParams.alt, mach));
                await RunSimulationTick();
            }

            // entering constant CAS stage of descent
            cas = route.Aircraft.DescentConstCAS;
            ffParams.vs = route.Aircraft.DescentConstCASVS;
            while (ffParams.alt > route.Aircraft.DescentXOverAltConstCAS)
            {
                ffParams.tas = (int)Math.Round(CAStoTAS(ffParams.alt, cas));
                await RunSimulationTick();
            }

            // entering final approach stage of descent
            phaseOfFlight = FlightPhase.FinalApproach;
            cas = route.Aircraft.FinalApproachCAS;
            ffParams.vs = route.Aircraft.FinalApproachVS;
            while (ffParams.alt > route.ArrivalAirport.altitude)
            {
                ffParams.tas = (int)Math.Round(CAStoTAS(ffParams.alt, cas));
                await RunSimulationTick();
            }

            remainingFuel = Math.Round(remainingFuel);

            Console.WriteLine($"remaining fuel after flight: {remainingFuel} kg");

            return 0;
        }
    */
    }
}
