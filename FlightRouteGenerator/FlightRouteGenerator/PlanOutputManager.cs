using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlightRouteGenerator
{
    internal class PlanOutputManager
    {
        public static void OutputRouteToConsole(Route route)
        {
            Console.WriteLine("\nYour route:\n");
            Console.WriteLine($"{route.DepartureAirport.ident}\n---------------");

            foreach (RouteLeg leg in route.Legs)
            {
                Console.WriteLine($"{leg.Airway.airwayName} {leg.Waypoint.ident} ({leg.Length:F1} nmi) \n---------------");
            }

            Console.WriteLine($"\n\nRoute in a format suitable for entry into a flight plotting tool:\n\n");
            Console.Write($"{route.DepartureAirport.ident} ");

            foreach (RouteLeg leg in route.Legs)
            {
                if (leg.Airway.isDirect)
                {
                    leg.Airway.airwayName = "DCT";
                }
                Console.Write($"{leg.Airway.airwayName} {leg.Waypoint.ident} ");
            }

            Console.WriteLine($"\n\nTotal distance: {route.TotalDistance:F1} nmi");
        }
    }
}
