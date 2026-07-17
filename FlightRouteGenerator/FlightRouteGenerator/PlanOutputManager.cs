using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FlightRouteGenerator
{
    internal class PlanOutputManager
    {

        private static Dictionary<string, Guid> KnownFolderGUIDs = new Dictionary<string, Guid> 
        {
            { "Downloads", new Guid("374DE290-123F-4565-9164-39C4925E467B") }
        };

        [DllImport("shell32", CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = false)]
        private static extern string SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, nint hToken = 0);

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

        public static void OutputRouteToFMSFile(Route route)
        {

            const string DIRECT = "DRCT";
            string wptAlt = "WPT_ALT_HERE";
            string fileContents = $"I\n1100 Version\nCYCLE {GLOBAL_SETTINGS.AIRAC_CYCLE}\nADEP " +
                $"{route.DepartureAirport.ident}\nADES {route.ArrivalAirport.ident}\nNUMENR {route.enrouteWaypointCount}\n" +
                $"{(int)WaypointType.Airport} {route.DepartureAirport.ident} " +
                $"ADEP {wptAlt} {route.DepartureAirport.laty:F6} {route.DepartureAirport.lonx:F6}\n";

            for (int i = 0; i < route.Legs.Count; i++)
            {
                RouteLeg leg = route.Legs[i];

                if (i < route.Legs.Count - 1)
                {
                    fileContents += $"{leg.Waypoint.Type} {leg.Waypoint.ident} {leg.Airway.airwayName} {wptAlt} " +
                        $"{leg.Waypoint.laty} {leg.Waypoint.lonx}\n";
                }
                else
                {
                    fileContents += $"{(int)WaypointType.Airport} {leg.Waypoint.ident} ADES {wptAlt} " +
                        $"{route.ArrivalAirport.laty:F6} {route.ArrivalAirport.lonx:F6}";
                }
            }

            string filePath = $"{SHGetKnownFolderPath(KnownFolderGUIDs["Downloads"], 0)}\\{route.DepartureAirport.ident}{route.ArrivalAirport.ident}.fms";

            using (StreamWriter writer = new StreamWriter(File.Open(filePath, FileMode.Create)))
            {
                writer.Write(fileContents);
            }
        }

        public static void OutputRouteToPLNFile(Route route)
        {

        }
    }
}
