using System.Runtime.InteropServices;
using System.Xml.Linq;

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
            // https://developer.x-plane.com/article/flightplan-files-v11-fms-file-format/

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
            // https://docs.flightsimulator.com/msfs2024/html/5_Content_Configuration/Mission_XML_Files/Flight_Plan_XML_Properties.htm

            double placeholderWptAlt = 67;
            double placeholderArptAlt = 42;
            string crzAlt = "CRZ_ALT_HERE";
            string planTitle = $"{route.DepartureAirport.ident} to {route.ArrivalAirport.ident}";
            string printableDepartureCoords = ConvertCoordinates.DecimalToDegMinSec(route.DepartureAirport.laty, route.DepartureAirport.lonx).GetPrintableString();

            string departureLLA_Assembly = $"{printableDepartureCoords},{placeholderArptAlt.ToString("000000.00")}";

            XElement simBase = new XElement("Simbase.Document",
                new XAttribute("Type", "AceXML"),
                new XAttribute("version", "1,0"),
                new XElement("Descr", "AceXML Document"),
                new XElement("FlightPlan.FlightPlan",
                    new XElement("Title", planTitle)),
                    new XElement("FPType", "VFR"),
                    new XElement("CruisingAlt", crzAlt),
                    new XElement("DepartureID", route.DepartureAirport),
                    new XElement("DepartureLLA", departureLLA_Assembly),
                    new XElement("DestinationID",route.ArrivalAirport.ident));

        }
    }
}
