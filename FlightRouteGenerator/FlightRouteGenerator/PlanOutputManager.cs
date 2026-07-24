using QuestPDF.Companion;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Runtime.InteropServices;
using System.Xml.Linq;

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
            Console.WriteLine($"{route.DepartureAirport.ident} at {route.DepartureAirport.altitude * M_TO_FT:F0} ft altitude\n---------------");

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

        public static string OutputRouteToFMSFile(Route route)
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

            return $"X-Plane route file:\nFlight plan exported as {fileName} to {filePath}\n";
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

        public static string OutputRouteToPLNFile(Route route)
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

            return $"Microsoft Flight Simulator route file:\nFlight plan exported as {fileName} to {filePath}\n";
        }

        public static string OutputRouteToPDFFile(Route route)
        {
            const double MpS_TO_KTS = 1.94384;
            // conversion factor from m/s to knots

            QuestPDF.Settings.License = LicenseType.Community;
            int remainingNumberOfLegsToOutput = route.Legs.Count;
            int legIndex = 0;

            foreach (RouteLeg leg in route.Legs)
            {
                if (leg.Airway.isDirect)
                {
                    leg.Airway.airwayName = GLOBAL_SETTINGS.DIRECT_FORMAT;
                }
            }



            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontFamily("Consolas").FontColor(Colors.Black).FontSize(12));

                    page.Header().Column(column =>
                    {
                        column.Item().Text($"{route.DepartureAirport.ident} - {route.ArrivalAirport.ident} ({route.Aircraft.ICAOIdent})")
                        .SemiBold().FontSize(18).FontColor(Colors.Black)
                        .AlignCenter()
                        .FontFamily("Consolas");

                        column.Item().PaddingBottom(2).Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.AlignRight();
                        });
                    });



                    page.Content()
                        .Border(3)
                        .BorderColor(Colors.Grey.Darken1)
                        .Padding(10)
                        .Column(column =>
                        {
                            column.Spacing(20);

                            column.Item().Padding(2).BorderBottom(1).BorderLeft(1).BorderRight(1).BorderTop(1).Background(Colors.Grey.Lighten1).Text($"OFP")
                                .FontSize(16)
                                .FontColor(Colors.Black)
                                .AlignCenter()
                                .SemiBold()
                                .FontFamily("Consolas");

                            column.Item().AlignRight().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(65);
                                    columns.ConstantColumn(65);
                                });

                                table.Cell().Text("CRZ SYS");
                                table.Cell().Text("CI 50").AlignRight();
                            });

                            //column.Item().AlignRight().PaddingTop(10).PaddingRight(10).Text("PIC SIGNATURE: ...............");

                            column.Item().Padding(2).BorderBottom(1).BorderLeft(1).BorderRight(1).BorderTop(1).Background(Colors.Grey.Lighten1).Text("PLANNED FUEL")
                                .FontSize(16)
                                .FontColor(Colors.Black)
                                .AlignCenter()
                                .SemiBold()
                                .FontFamily("Consolas");

                            column.Item().Padding(2).BorderBottom(1).BorderLeft(1).BorderRight(1).BorderTop(1).Background(Colors.Grey.Lighten1).Text("LOADSHEET")
                                .FontSize(16)
                                .FontColor(Colors.Black)
                                .AlignCenter()
                                .SemiBold()
                                .FontFamily("Consolas");

                            column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(80);
                                        columns.ConstantColumn(65);
                                        columns.ConstantColumn(65);
                                        columns.ConstantColumn(65);
                                        columns.ConstantColumn(120);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Element(CellStyle);
                                        header.Cell().Element(CellStyle).Text("EST");
                                        header.Cell().Element(CellStyle).Text("MAX");
                                        header.Cell().Element(CellStyle).Text("ACTUAL");
                                        header.Cell();

                                        static IContainer CellStyle(IContainer container)
                                        {
                                            return container
                                                .DefaultTextStyle(x => x.FontFamily("Consolas").FontSize(12).SemiBold())
                                                .BorderBottom(1)
                                                .AlignCenter()
                                                .Padding(4);
                                        }
                                    });

                                    static IContainer CellStyle(IContainer container)
                                    {
                                        return container
                                            .DefaultTextStyle(x => x.FontFamily("Consolas").FontSize(12))
                                            .AlignCenter()
                                            .Padding(4);
                                    }

                                    table.Cell().BorderRight(1).Element(CellStyle).Text($"PAX");
                                    table.Cell().Element(CellStyle).Text($"{route.Loadsheet.Pax}");
                                    table.Cell().Element(CellStyle).Text("");
                                    table.Cell().Element(CellStyle).Padding(8).Text("......");
                                    table.Cell();

                                    table.Cell().BorderRight(1).Element(CellStyle).Text("BAG/CARGO");
                                    table.Cell().Element(CellStyle).Text($"{Math.Round(route.Loadsheet.BagsAndCargo / 1000):F1}");
                                    table.Cell().Element(CellStyle).Text("");
                                    table.Cell().Element(CellStyle).Padding(8).Text("......");
                                    table.Cell();

                                    table.Cell().BorderRight(1).Element(CellStyle).Text("PAYLOAD");
                                    table.Cell().Element(CellStyle).Text($"{Math.Round(route.Loadsheet.Payload / 1000):F1}");
                                    table.Cell().Element(CellStyle).Text("");
                                    table.Cell().Element(CellStyle).Padding(8).Text("......");
                                    table.Cell();

                                    table.Cell().BorderRight(1).Element(CellStyle).Text("ZFW");
                                    table.Cell().Element(CellStyle).Text($"{Math.Round(route.Loadsheet.ZFW / 1000):F1}");
                                    table.Cell().Element(CellStyle).Text($"{(route.Aircraft.MTOW - route.Loadsheet.BlockFuel) / 1000:F1}");
                                    table.Cell().Element(CellStyle).Padding(8).Text("......");
                                    table.Cell();

                                    double maxFuel = Math.Min(route.Aircraft.MTOW - route.Loadsheet.ZFW, route.Aircraft.MaxFuelCapacity);

                                    table.Cell().BorderRight(1).Element(CellStyle).Text("FUEL");
                                    table.Cell().Element(CellStyle).Text($"{Math.Round(route.Loadsheet.BlockFuel / 1000):F1}");
                                    table.Cell().Element(CellStyle).Text($"{maxFuel / 1000:F1}");
                                    table.Cell().Element(CellStyle).Padding(8).Text("......");
                                    table.Cell().Text($"POSS EXTRA {(maxFuel - route.Loadsheet.BlockFuel) / 1000:F1}");

                                    table.Cell().BorderRight(1).Element(CellStyle).Text("TOW");
                                    table.Cell().Element(CellStyle).Text($"{Math.Round(route.Loadsheet.TOW / 1000):F1}");
                                    table.Cell().Element(CellStyle).Text($"{route.Aircraft.MTOW / 1000:F1}");
                                    table.Cell().Element(CellStyle).Padding(8).Text("......");
                                    table.Cell();

                                    table.Cell().BorderRight(1).Element(CellStyle).Text("LAW");
                                    table.Cell().Element(CellStyle).Text($"{Math.Round(route.Loadsheet.LAW / 1000):F1}");
                                    table.Cell().Element(CellStyle).Text($"{route.Aircraft.MLW / 1000:F1}");
                                    table.Cell().Element(CellStyle).Padding(8).Text("......");
                                    table.Cell();
                                });

                            column.Item().Padding(2).BorderBottom(1).BorderLeft(1).BorderRight(1).BorderTop(1).Background(Colors.Grey.Lighten1).Text("ROUTE")
                                .FontSize(16)
                                .FontColor(Colors.Black)
                                .AlignCenter()
                                .SemiBold()
                                .FontFamily("Consolas");

                            const double M_TO_FT = 3.28084;

                            void AddLegRow(TableDescriptor table, RouteLeg leg, string legType, int legIndex)
                            {
                                switch (legType)
                                {
                                    case "departureAirport":
                                        table.Cell()
                                           .BorderRight(1).Padding(4)
                                           .Text($"----\n{route.DepartureAirport.name.ToUpper()}\n{route.DepartureAirport.ident}").FontFamily("Consolas").FontSize(12);
                                        table.Cell()
                                            .Padding(4)
                                            .Text($"\n{ConvertCoordinates.GetPDFDecimalLatyFormat(route.DepartureAirport.laty)}\n{ConvertCoordinates.GetPDFDecimalLonxFormat(route.DepartureAirport.lonx)}").AlignCenter().FontFamily("Consolas").FontSize(12);
                                        table.Cell()
                                            .Padding(4)
                                            .Text($"\n{Math.Round(route.DepartureAirport.altitude * M_TO_FT / 100).ToString("000")}").AlignCenter().FontFamily("Consolas").FontSize(12);
                                        table.Cell()
                                            .Padding(4)
                                            .Text($"\n---").FontFamily("Consolas").AlignCenter().FontSize(12);
                                        table.Cell()
                                            .Padding(4)
                                            .Text($"\n---").FontFamily("Consolas").AlignCenter().FontSize(12);
                                        break;
                                    case "arrivalAirport":
                                        table.Cell()
                                           .BorderBottom(1)
                                           .BorderRight(1).Padding(4)
                                           .Text($"----\n{route.ArrivalAirport.name.ToUpper()}\n{route.ArrivalAirport.ident}").FontFamily("Consolas").FontSize(12);
                                        table.Cell()
                                            .BorderBottom(1)
                                            .Padding(4)
                                            .Text($"\n{ConvertCoordinates.GetPDFDecimalLatyFormat(route.ArrivalAirport.laty)}\n{ConvertCoordinates.GetPDFDecimalLonxFormat(route.ArrivalAirport.lonx)}").AlignCenter().FontFamily("Consolas").FontSize(12);
                                        table.Cell()
                                            .BorderBottom(1)
                                            .Padding(4)
                                            .Text($"\n{Math.Round(route.ArrivalAirport.altitude * M_TO_FT / 100).ToString("000")}").AlignCenter().FontFamily("Consolas").FontSize(12);
                                        table.Cell()
                                            .BorderBottom(1)
                                            .Padding(4)
                                            .Text($"\n---").FontFamily("Consolas").AlignCenter().FontSize(12);
                                        table.Cell()
                                            .BorderBottom(1)
                                            .Padding(4)
                                            .Text($"\n---").FontFamily("Consolas").AlignCenter().FontSize(12);
                                        break;
                                    default:
                                        table.Cell()
                                            .BorderRight(1).Padding(4)
                                            .Text($"{leg.Airway.airwayName}\n{leg.Waypoint.Name}\n{leg.Waypoint.ident}").FontFamily("Consolas").FontSize(12);
                                        table.Cell()
                                            .Padding(4)
                                            .Text($"\n{ConvertCoordinates.GetPDFDecimalLatyFormat(leg.Waypoint.laty)}\n{ConvertCoordinates.GetPDFDecimalLonxFormat(leg.Waypoint.lonx)}").AlignCenter().FontFamily("Consolas").FontSize(12);
                                        table.Cell()
                                            .Padding(4)
                                            .Text($"\n{Math.Round((double)leg.Waypoint.Altitude / 100).ToString("000")}").AlignCenter().FontFamily("Consolas").FontSize(12);
                                        table.Cell()
                                            .Padding(4)
                                            .Text($"\n{leg.Waypoint.TAS * MpS_TO_KTS:F0}").FontFamily("Consolas").AlignCenter().FontSize(12);
                                        table.Cell()
                                            .Padding(4)
                                            .Text($"\n{leg.Waypoint.OAT - 273.15:F0}").FontFamily("Consolas").AlignCenter().FontSize(12);

                                        if (legIndex == route.TC_Info.PreviousLegIndex)
                                        {
                                                table.Cell()
                                                .BorderRight(1).Padding(4)
                                                .Text($"{GLOBAL_SETTINGS.DIRECT_FORMAT}\nT O C\n").FontFamily("Consolas").FontSize(12);
                                            table.Cell()
                                                .Padding(4)
                                                .Text($"\n{ConvertCoordinates.GetPDFDecimalLatyFormat(route.TC_Info.laty)}\n{ConvertCoordinates.GetPDFDecimalLonxFormat(route.TC_Info.lonx)}").AlignCenter().FontFamily("Consolas").FontSize(12);
                                            table.Cell()
                                                .Padding(4)
                                                .Text($"\n{Math.Round((double)route.TC_Info.Altitude * M_TO_FT / 100).ToString("000")}").AlignCenter().FontFamily("Consolas").FontSize(12);
                                            table.Cell()
                                                .Padding(4)
                                                .Text($"\n{route.TC_Info.TAS * MpS_TO_KTS:F0}").FontFamily("Consolas").AlignCenter().FontSize(12);
                                            table.Cell()
                                                .Padding(4)
                                                .Text($"\n{route.TC_Info.OAT - 273.15:F0}").FontFamily("Consolas").AlignCenter().FontSize(12);
                                        }
                                        if (legIndex == route.TD_Info.PreviousLegIndex)
                                        {
                                            table.Cell()
                                                .BorderRight(1).Padding(4)
                                                .Text($"{GLOBAL_SETTINGS.DIRECT_FORMAT}\nT O D\n").FontFamily("Consolas").FontSize(12);
                                            table.Cell()
                                                .Padding(4)
                                                .Text($"\n{ConvertCoordinates.GetPDFDecimalLatyFormat(route.TD_Info.laty)}\n{ConvertCoordinates.GetPDFDecimalLonxFormat(route.TD_Info.lonx)}").AlignCenter().FontFamily("Consolas").FontSize(12);
                                            table.Cell()
                                                .Padding(4)
                                                .Text($"\n{Math.Round((double)route.TD_Info.Altitude * M_TO_FT / 100).ToString("000")}").AlignCenter().FontFamily("Consolas").FontSize(12);
                                            table.Cell()
                                                .Padding(4)
                                                .Text($"\n{route.TD_Info.TAS * MpS_TO_KTS:F0}").FontFamily("Consolas").AlignCenter().FontSize(12);
                                            table.Cell()
                                                .Padding(4)
                                                .Text($"\n{route.TD_Info.OAT - 273.15:F0}").FontFamily("Consolas").AlignCenter().FontSize(12);
                                        }
                                        break;
                                }

                            }

                            static IContainer CellStyle(IContainer container)
                            {
                                return container
                                    .DefaultTextStyle(x => x.FontFamily("Consolas").FontSize(12).SemiBold())
                                    .BorderBottom(1)
                                    .AlignCenter()
                                    .Padding(4);
                            }

                            column.Item().Text($"Total ground distance: {route.TotalDistance:F0} nmi").FontFamily("Consolas").FontSize(12).AlignLeft();

                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(100);
                                    columns.ConstantColumn(100);
                                    columns.ConstantColumn(100);
                                    columns.ConstantColumn(100);
                                    columns.ConstantColumn(100);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().BorderBottom(1).AlignCenter().Padding(4).Text("AIRWAY\nNAME\nIDENT").FontFamily("Consolas").FontSize(12).SemiBold().AlignCenter();
                                    header.Cell().BorderBottom(1).AlignCenter().Padding(4).Text("\nLAT\nLONG").FontFamily("Consolas").FontSize(12).SemiBold().AlignCenter();
                                    header.Cell().BorderBottom(1).AlignCenter().Padding(4).Text("\n\nFL").FontFamily("Consolas").FontSize(12).SemiBold().AlignCenter();
                                    header.Cell().BorderBottom(1).AlignCenter().Padding(4).Text("\n\nTAS").FontFamily("Consolas").FontSize(12).SemiBold().AlignCenter();
                                    header.Cell().BorderBottom(1).AlignCenter().Padding(4).Text("\n\nOAT").FontFamily("Consolas").FontSize(12).SemiBold().AlignCenter();
                                });

                                AddLegRow(table, route.Legs[0], legType: "departureAirport", -1);

                                for (int i = 1; i < route.Legs.Count - 1; i++)
                                {
                                    AddLegRow(table, route.Legs[i], legType:"", i);
                                }

                                AddLegRow(table, route.Legs[route.Legs.Count - 1], legType: "arrivalAirport", route.Legs.Count + 1);
                            });
                        });
                });

            });

            bool debugLiveView;
#if DEBUG
            debugLiveView = true;
#else
            debugLiveView = false;
#endif

            if (debugLiveView)
            {
                document.ShowInCompanion(12500);
                return "debugpdf";
            }
            else
            {
                string fileName = $"{route.DepartureAirport.ident}{route.ArrivalAirport.ident}_Plan.pdf";
                string filePath = $"{SHGetKnownFolderPath(KnownFolderGUIDs["Downloads"], 0)}\\{fileName}";

                using FileStream stream = new FileStream(filePath, FileMode.Create);
                document.GeneratePdf(stream);

                return $"PDF File:\nFlight plan exported as {fileName} to {filePath}\n";
            }
        }
    }
}

