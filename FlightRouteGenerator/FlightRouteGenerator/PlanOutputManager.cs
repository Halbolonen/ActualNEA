using System.Runtime.InteropServices;
using System.Xml.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Companion;

namespace FlightRouteGenerator
{

    internal class PlanOutputManager
    {
        private static void DrawLine()
        {
            for (int i = 0; i < Console.WindowWidth; i++)
            {
                Console.Write('-');
            }
            Console.WriteLine();
        }

        private static double M_TO_FT = 3.28084;

        private static Dictionary<string, Guid> KnownFolderGUIDs = new Dictionary<string, Guid>
        {
            { "Downloads", new Guid("374DE290-123F-4565-9164-39C4925E467B") }
        };

        [DllImport("shell32", CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = false)]
        private static extern string SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, nint hToken = 0);

        public static void OutputRouteToConsole(Route route)
        {
            Console.WriteLine("\nYour route:\n");
            Console.WriteLine($"{route.DepartureAirport.ident} at {Math.Round(route.DepartureAirport.altitude * M_TO_FT)} ft altitude\n---------------");

            foreach (RouteLeg leg in route.Legs)
            {
                if (leg.Airway.isDirect)
                {
                    leg.Airway.airwayName = GLOBAL_SETTINGS.DIRECT_FORMAT;
                }
                Console.WriteLine($"{leg.Airway.airwayName} {leg.Waypoint.ident} at {leg.Waypoint.Altitude} ft altitude (leg length {leg.Length:F1} nmi) \n---------------");
            }

            Console.WriteLine($"\n\nRoute in a format suitable for entry into a flight plotting tool:\n\n");
            Console.Write($"{route.DepartureAirport.ident} ");

            foreach (RouteLeg leg in route.Legs)
            {
                if (leg.Airway.isDirect)
                {
                    leg.Airway.airwayName = GLOBAL_SETTINGS.DIRECT_FORMAT;
                }
                Console.Write($"{leg.Airway.airwayName} {leg.Waypoint.ident} ");
            }

            Console.WriteLine($"\n\nTotal distance: {route.TotalDistance:F1} nmi");

            DrawLine();
            Console.WriteLine("\nLOADSHEET DATA BELOW\n");
            DrawLine();
            Console.WriteLine();

            Console.WriteLine(
@$"PAX: {route.Loadsheet.Pax}
BLOCK FUEL: {route.Loadsheet.BlockFuel} kg
BAGS/CARGO: {route.Loadsheet.BagsAndCargo} kg
PAYLOAD: {route.Loadsheet.Payload} kg
TOW: {route.Loadsheet.TOW} kg
LAW: {route.Loadsheet.LAW} kg
ZFW: {route.Loadsheet.ZFW} kg
                ");
        }

        public static void OutputRouteToFMSFile(Route route)
        {
            // https://developer.x-plane.com/article/flightplan-files-v11-fms-file-format/

            string fileName = $"{route.DepartureAirport.ident}{route.ArrivalAirport.ident}.fms";
            string filePath = $"{SHGetKnownFolderPath(KnownFolderGUIDs["Downloads"], 0)}\\{fileName}";

            const string DIRECT = "DRCT";
            string fileContents = $"I\n1100 Version\nCYCLE {GLOBAL_SETTINGS.AIRAC_CYCLE}\nADEP " +
                $"{route.DepartureAirport.ident}\nADES {route.ArrivalAirport.ident}\nNUMENR {route.enrouteWaypointCount}\n" +
                $"{(int)WaypointType.Airport} {route.DepartureAirport.ident} " +
                $"ADEP {route.DepartureAirport.altitude:F6} {route.DepartureAirport.laty:F6} {route.DepartureAirport.lonx:F6}\n";

            for (int i = 0; i < route.Legs.Count; i++)
            {
                RouteLeg leg = route.Legs[i];

                if (i < route.Legs.Count - 1)
                {
                    fileContents += $"{leg.Waypoint.Type} {leg.Waypoint.ident} {leg.Airway.airwayName} {leg.Waypoint.Altitude:F6} " +
                        $"{leg.Waypoint.laty} {leg.Waypoint.lonx}\n";
                }
                else
                {
                    fileContents += $"{(int)WaypointType.Airport} {leg.Waypoint.ident} ADES {route.ArrivalAirport.altitude:F6} " +
                        $"{route.ArrivalAirport.laty:F6} {route.ArrivalAirport.lonx:F6}";
                }
            }

            using (StreamWriter writer = new StreamWriter(File.Open(filePath, FileMode.Create)))
            {
                writer.Write(fileContents);
            }

            Console.WriteLine($"X-Plane route file:\nFlight plan exported as {fileName} to {filePath}\n");
        }

        private static string GenerateLLA(double laty, double lonx, double altitude)
        {
            string printableDMSCoords = ConvertCoordinates.DecimalToDegMinSec(
                laty, lonx).GetPrintableString();

            string formattedAltitude = altitude.ToString("000000.00");
            if (altitude > 0)
            {
                formattedAltitude = "+" + formattedAltitude;
            }

            return $"{printableDMSCoords},{formattedAltitude}";
        }

        private static XElement GenerateATCWaypointXElement(string ident, double laty, double lonx, AirwayRecord airway, int type, double altitude)
        {
            PLNWaypointInfo waypointInfo = new PLNWaypointInfo();

            switch (type)
            {
                case (int)WaypointType.NamedFix:
                    waypointInfo.ATCWaypointType = "Intersection";
                    break;

                case (int)WaypointType.VOR:
                    waypointInfo.ATCWaypointType = "VOR";
                    break;

                case (int)WaypointType.NDB:
                    waypointInfo.ATCWaypointType = "NDB";
                    break;
            }

            if (airway.airwayName != AirwayRecord.DEFAULT_DIRECT_FORMAT)
            {
                waypointInfo.ATCAirway = airway.airwayName;
            }
            // if the airway is not direct, leave the field undefined.

            waypointInfo.WorldPositionLLA = GenerateLLA(laty, lonx, altitude);

            XElement atcWaypoint = new XElement("ATCWaypoint", new XAttribute("id", ident),
                new XElement("ATCWaypointType", waypointInfo.ATCWaypointType),
                new XElement("WorldPosition", waypointInfo.WorldPositionLLA)
                );

            if (waypointInfo.ATCAirway != null)
            {
                atcWaypoint.Add(new XElement("ATCAirway", waypointInfo.ATCAirway));
            }

            return atcWaypoint;
        }

        private static XElement GenerateAirportATCWaypointXElement(AirportRecord airport, double altitude)
        {
            PLNWaypointInfo airportWaypointInfo = new PLNWaypointInfo();

            airportWaypointInfo.ATCWaypointType = "Airport";
            airportWaypointInfo.WorldPositionLLA = GenerateLLA(airport.laty, airport.lonx, altitude);

            XElement atcWaypoint = new XElement("ATCWaypoint", new XAttribute("id", airport.ident),
                new XElement("ATCWaypointType", airportWaypointInfo.ATCWaypointType),
                new XElement("WorldPosition", airportWaypointInfo.WorldPositionLLA)
            );

            return atcWaypoint;
        }

        private static XElement GenerateRouteLegATCWaypointXElement(RouteLeg leg, double altitude)
        {
            return GenerateATCWaypointXElement(leg.Waypoint.ident, leg.Waypoint.laty, leg.Waypoint.lonx, leg.Airway, (int)leg.Waypoint.Type, altitude);
        }

        public static void OutputRouteToPLNFile(Route route)
        {
            // https://docs.flightsimulator.com/msfs2024/html/5_Content_Configuration/Mission_XML_Files/Flight_Plan_XML_Properties.htm
            // LLA stands for Latitude, Longitude, Altitude and is a string collection of the former.

            string fileContents = "";
            string fileName = $"{route.DepartureAirport.ident}{route.ArrivalAirport.ident}.pln";
            string filePath = $"{SHGetKnownFolderPath(KnownFolderGUIDs["Downloads"], 0)}\\{fileName}";

            string planTitle = $"{route.DepartureAirport.ident} to {route.ArrivalAirport.ident}";

            string departureLLA = GenerateLLA(route.DepartureAirport.laty, route.DepartureAirport.lonx, route.DepartureAirport.altitude);
            string arrivalLLA = GenerateLLA(route.ArrivalAirport.laty, route.ArrivalAirport.lonx, route.DepartureAirport.altitude);

            Dictionary<string, PLNWaypointInfo> waypointInfoDict = new Dictionary<string, PLNWaypointInfo>();

            XElement simBase = new XElement("Simbase.Document",
                new XAttribute("Type", "AceXML"),
                new XAttribute("version", "1,0"),
                new XElement("Descr", "AceXML Document")
            );

            XElement flightPlan = new XElement("FlightPlan.FlightPlan",
                new XElement("Title", planTitle),
                new XElement("FPType", "IFR"),
                new XElement("CruisingAlt", route.CruiseAltitude * M_TO_FT),
                new XElement("DepartureID", route.DepartureAirport.ident),
                new XElement("DepartureLLA", departureLLA),
                new XElement("DestinationID", route.ArrivalAirport.ident),
                new XElement("DestinationLLA", arrivalLLA),
                new XElement("Descr", $"{route.DepartureAirport.ident} to {route.ArrivalAirport.ident}, generated by CompSci NEA Flight Route Generator™"),
                new XElement("DestinationName", route.ArrivalAirport.name),
                new XElement("AppVersion",
                    new XElement("AppVersionMajor", "11")
                    )
                );

            flightPlan.Add(GenerateAirportATCWaypointXElement(route.DepartureAirport, route.DepartureAirport.altitude));

            foreach (RouteLeg leg in route.Legs)
            {
                if (!leg.isAirportLeg)
                {
                    flightPlan.Add(GenerateRouteLegATCWaypointXElement(leg, leg.Waypoint.Altitude));
                }
            }

            flightPlan.Add(GenerateAirportATCWaypointXElement(route.ArrivalAirport, route.ArrivalAirport.altitude));
            simBase.Add(flightPlan);

            XDocument planDoc = new XDocument(new XDeclaration("1.0", "UTF-8", null), simBase);
            planDoc.Save(filePath);

            Console.WriteLine($"Microsoft Flight Simulator route file:\nFlight plan exported as {fileName} to {filePath}\n");
        }

        public static void OutputRouteToPDFFile(Route route)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(20));

                    page.Header()
                        .Text($"{route.DepartureAirport.ident} - {route.ArrivalAirport.ident}")
                        .SemiBold().FontSize(18).FontColor(Colors.Black)
                        .AlignCenter()
                        .FontFamily("Consolas");

                    page.Content()
                        .Border(3)
                        .BorderColor(Colors.Grey.Darken1)
                        .Padding(10)
                        .Column(column =>
                        {
                            column.Spacing(20);

                            column.Item().Text("LOADSHEET")
                                .FontSize(14)
                                .FontColor(Colors.Black)
                                .AlignCenter()
                                .FontFamily("Consolas");

                            column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(100);
                                        columns.ConstantColumn(65);
                                        columns.ConstantColumn(65);
                                        columns.ConstantColumn(65);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Padding(4).Text("").FontFamily("Consolas").FontSize(12);
                                        header.Cell().Padding(4).Text("EST").FontFamily("Consolas").FontSize(12);
                                        header.Cell().Padding(4).Text("MAX").FontFamily("Consolas").FontSize(12);
                                        header.Cell().Padding(4).Text("ACTUAL").FontFamily("Consolas").FontSize(12);
                                    });

                                    table.Cell()
                                        .Padding(4)
                                        .Text("PAX").FontFamily("Consolas").FontSize(12);
                                    table.Cell()
                                        .Padding(4)
                                        .Text($"{route.Loadsheet.Pax}").FontFamily("Consolas").FontSize(12);
                                    table.Cell()
                                        .Padding(4)
                                        .Text($"{"!IMP"}").FontFamily("Consolas").FontSize(12);
                                    table.Cell()
                                        .Padding(4)
                                        .Text("......").FontFamily("Consolas").FontSize(12);

                                    table.Cell()
                                        .Padding(4)
                                        .Text("BAG/CARGO").FontFamily("Consolas").FontSize(12);
                                    table.Cell()
                                        .Padding(4)
                                        .Text($"{route.Loadsheet.BagsAndCargo}").FontFamily("Consolas").FontSize(12);
                                    table.Cell()
                                        .Padding(4)
                                        .Text($"{"!IMP"}").FontFamily("Consolas").FontSize(12);
                                    table.Cell()
                                        .Padding(4)
                                        .Text("......").FontFamily("Consolas").FontSize(12);

                                    table.Cell()
                                        .Padding(4)
                                        .Text("PAYLOAD").FontFamily("Consolas").FontSize(12);
                                    table.Cell()
                                        .Padding(4)
                                        .Text($"{route.Loadsheet.Payload}").FontFamily("Consolas").FontSize(12);
                                    table.Cell()
                                        .Padding(4)
                                        .Text($"{"!IMP"}").FontFamily("Consolas").FontSize(12);
                                    table.Cell()
                                        .Padding(4)
                                        .Text("......").FontFamily("Consolas").FontSize(12);

                                    table.Cell()
                                        .Padding(4)
                                        .Text("ZFW").FontFamily("Consolas").FontSize(12);
                                    table.Cell()
                                        .Padding(4)
                                        .Text($"{route.Loadsheet.ZFW}").FontFamily("Consolas").FontSize(12);
                                    table.Cell()
                                        .Padding(4)
                                        .Text($"{"!IMP"}").FontFamily("Consolas").FontSize(12);
                                    table.Cell()
                                        .Padding(4)
                                        .Text("......").FontFamily("Consolas").FontSize(12);

                                    table.Cell()
                                        .Padding(4)
                                        .Text("FUEL").FontFamily("Consolas").FontSize(12);
                                    table.Cell()
                                        .Padding(4)
                                        .Text($"{route.Loadsheet.BlockFuel}").FontFamily("Consolas").FontSize(12);
                                    table.Cell()
                                        .Padding(4)
                                        .Text($"{"!IMP"}").FontFamily("Consolas").FontSize(12);
                                    table.Cell()
                                        .Padding(4)
                                        .Text("......").FontFamily("Consolas").FontSize(12);

                                    table.Cell()
                                        .Padding(4)
                                        .Text("TOW").FontFamily("Consolas").FontSize(12);
                                    table.Cell()
                                        .Padding(4)
                                        .Text($"{route.Loadsheet.TOW}").FontFamily("Consolas").FontSize(12);
                                    table.Cell()
                                        .Padding(4)
                                        .Text($"{"!IMP"}").FontFamily("Consolas").FontSize(12);
                                    table.Cell()
                                        .Padding(4)
                                        .Text("......").FontFamily("Consolas").FontSize(12);

                                    table.Cell()
                                        .Padding(4)
                                        .Text("LAW").FontFamily("Consolas").FontSize(12);
                                    table.Cell()
                                        .Padding(4)
                                        .Text($"{route.Loadsheet.LAW}").FontFamily("Consolas").FontSize(12);
                                    table.Cell()
                                        .Padding(4)
                                        .Text($"{"!IMP"}").FontFamily("Consolas").FontSize(12);
                                    table.Cell()
                                        .Padding(4)
                                        .Text("......").FontFamily("Consolas").FontSize(12);
                                });
                    });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                        });
                });
                
            });

            document.ShowInCompanion(12500);
        }
    }
}
