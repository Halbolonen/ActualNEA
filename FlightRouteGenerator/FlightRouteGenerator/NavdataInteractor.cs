using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace FlightRouteGenerator
{
    internal static class NavdataInteractor
    {
#if DEBUG
        private static string NAV_DB_FILE_PATH = $"Data Source=\"{Directory.GetCurrentDirectory()}\\..\\Data\\navdata.sqlite\";";
#else
        private static string NAV_DB_FILE_PATH = $"Data Source=\"{Directory.GetCurrentDirectory()}\\Data\\navdata.sqlite\";";
#endif
        private static SQLiteConnection navDBConnection = new SQLiteConnection(NAV_DB_FILE_PATH);
        public static Dictionary<string, WaypointRecord> waypointRecordDict { get; private set; }
        public static Dictionary<string, AirportRecord> airportRecordDict { get; private set; }
        public static Dictionary<string, AirwayRecord> airwayRecordDict { get; private set; }
        public static Dictionary<string, VORRecord> vorRecordDict { get; private set; }
        public static Dictionary<string, NDBRecord> ndbRecordDict { get; private set; }
        public static Dictionary<string, List<(WaypointRecord, AirwayRecord)>> outgoingAirwaysByWaypointID { get; set; }
        private static HashSet<string> connectedWaypointIDs = new HashSet<string>();
        public static bool Initialised { get; private set; }

        private static void LoadRecords(string typeOfRecord)
        {
            navDBConnection.Open();
            string command = "";
            switch (typeOfRecord)
            {
                case "waypoint":
                    command =
                @"SELECT waypoint_id, ident, lonx, laty, name, nav_id
                FROM waypoint";
                    break;

                case "airport":
                    command =
                @"SELECT airport_id, ident, name, lonx, laty, altitude
                FROM airport";
                    break;

                case "airway":
                    command = @"SELECT airway_id, airway_name, from_waypoint_id, to_waypoint_id,
                from_laty, from_lonx, to_laty, to_lonx
                FROM airway
                WHERE airway.direction = 'F'
                OR airway.direction = 'N'";
                    break;

                case "vor":
                    command = @"SELECT vor_id, ident, name
                    FROM vor";
                    break;

                case "ndb":
                    command = @"SELECT ndb_id, ident, name FROM ndb";
                    break;

                default:
                    throw new Exception("invalid LoadRecords argument.");
            }

            SQLiteDataReader dataReader;
            SQLiteCommand commandObject = new SQLiteCommand(command, navDBConnection);

            dataReader = commandObject.ExecuteReader();

            while (dataReader.Read())
            {

                switch (typeOfRecord)
                {
                    case "waypoint":
                        WaypointRecord wpRecord = new WaypointRecord();

                        wpRecord.WaypointID = Convert.ToString(dataReader["waypoint_id"]);
                        wpRecord.ident = (string)dataReader["ident"];
                        wpRecord.laty = Convert.ToDouble(dataReader["laty"]);
                        wpRecord.lonx = Convert.ToDouble(dataReader["lonx"]);
                        if (!dataReader.IsDBNull(4))
                        {
                            wpRecord.Name = (string)dataReader["name"];
                        }
                        else
                        {
                            wpRecord.Name = "";
                        }

                        if (!dataReader.IsDBNull(5))
                        {
                            wpRecord.NavID = Convert.ToString(dataReader["nav_id"]);
                        }

                        waypointRecordDict.Add(wpRecord.WaypointID, wpRecord);
                        break;

                    case "airport":
                        AirportRecord apRecord = new AirportRecord();
                        // modify the globals instead of recordDict

                        apRecord.AirportID = Convert.ToString(dataReader["airport_id"]);
                        apRecord.ident = (string)dataReader["ident"];
                        apRecord.name = (string)dataReader["name"];
                        apRecord.laty = Convert.ToDouble(dataReader["laty"]);
                        apRecord.lonx = Convert.ToDouble(dataReader["lonx"]);
                        apRecord.altitude = (int)(Convert.ToInt32(dataReader["altitude"]) / 3.281);
                        // converting altitude from feet to metres

                        airportRecordDict.Add(apRecord.AirportID, apRecord);
                        break;

                    case "airway":
                        AirwayRecord awRecord = new AirwayRecord();

                        awRecord.AirwayID = Convert.ToString(dataReader["airway_id"]);
                        awRecord.airwayName = (string)dataReader["airway_name"];
                        awRecord.fromWaypointID = Convert.ToString(dataReader["from_waypoint_id"]);
                        awRecord.toWaypointID = Convert.ToString(dataReader["to_waypoint_id"]);
                        awRecord.fromLaty = Convert.ToDouble(dataReader["from_laty"]);
                        awRecord.fromLonx = Convert.ToDouble(dataReader["from_lonx"]);
                        awRecord.toLaty = Convert.ToDouble(dataReader["to_laty"]);
                        awRecord.toLonx = Convert.ToDouble(dataReader["to_lonx"]);
                        awRecord.length = Navigator.GetDistanceBetweenGeoCoordinates(
                            awRecord.fromLaty, awRecord.fromLonx, awRecord.toLaty, awRecord.toLonx
                            );

                        connectedWaypointIDs.Add(awRecord.fromWaypointID);
                        connectedWaypointIDs.Add(awRecord.toWaypointID);

                        airwayRecordDict.Add(awRecord.AirwayID, awRecord);
                        break;

                    case "vor":
                        VORRecord vorRecord = new VORRecord();

                        vorRecord.VOR_ID = Convert.ToString(dataReader["vor_id"]);
                        vorRecord.ident = (string)dataReader["ident"];
                        vorRecord.name = (string)dataReader["name"];

                        vorRecordDict.Add(vorRecord.VOR_ID, vorRecord);
                        break;

                    case "ndb":
                        NDBRecord ndbRecord = new NDBRecord();

                        ndbRecord.NDB_ID = Convert.ToString(dataReader["ndb_id"]);
                        ndbRecord.ident = (string)dataReader["ident"];
                        ndbRecord.name = (string)dataReader["name"];

                        ndbRecordDict.Add(ndbRecord.NDB_ID, ndbRecord);
                        break;
                }
            }

            navDBConnection.Close();
        }

        public static void LoadWaypointRecords()
        {
            LoadRecords("waypoint");
        }

        public static void LoadAirportRecords()
        {
            LoadRecords("airport");
        }

        public static void LoadAirwayRecords()
        {
            LoadRecords("airway");
        }

        public static void LoadVORIdentHashSet()
        {
            LoadRecords("vor");
        }

        public static void LoadNDBIdentHashSet()
        {
            LoadRecords("ndb");
        }

        public static void Initialise()
        {
            waypointRecordDict = new Dictionary<string, WaypointRecord>();
            airportRecordDict = new Dictionary<string, AirportRecord>();
            airwayRecordDict = new Dictionary<string, AirwayRecord>();
            vorRecordDict = new Dictionary<string, VORRecord>();
            ndbRecordDict = new Dictionary<string, NDBRecord>();

            LoadWaypointRecords();
            LoadAirportRecords();
            LoadAirwayRecords();
            LoadVORIdentHashSet();
            LoadNDBIdentHashSet();

            foreach (Record record in waypointRecordDict.Values)
            {
                WaypointRecord wpRecord = (WaypointRecord)record;
                if (!connectedWaypointIDs.Contains(wpRecord.WaypointID))
                {
                    waypointRecordDict.Remove(wpRecord.WaypointID);
                }

                if (wpRecord.NavID != "" && wpRecord.NavID != null)
                {
                    bool navTypeDecided = false;
                    // check against ndb and vor
                    NDBRecord ndbRecord;

                    if (ndbRecordDict.TryGetValue(wpRecord.NavID, out ndbRecord))
                    {
                        if (ndbRecord.ident == wpRecord.ident)
                        {
                            wpRecord.Type = (int)WaypointType.NDB;
                            wpRecord.Name = ndbRecord.name;
                            navTypeDecided = true;
                        }
                    }

                    if (!navTypeDecided)
                    {
                        VORRecord vorRecord;

                        if (vorRecordDict.TryGetValue(wpRecord.NavID, out vorRecord))
                        {
                            if (vorRecord.ident == wpRecord.ident)
                            {
                                wpRecord.Type = (int)WaypointType.VOR;
                                wpRecord.Name = vorRecord.name;
                                navTypeDecided = true;
                            }
                        }
                    }

                    if (!navTypeDecided)
                    {
                        wpRecord.Name = "";
                    }

                }
                else
                {
                    wpRecord.Type = (int)WaypointType.NamedFix;
                } 
            }

            outgoingAirwaysByWaypointID = new Dictionary<string, List<(WaypointRecord, AirwayRecord)>>();
            WaypointRecord toWaypoint = new WaypointRecord();

            foreach (AirwayRecord airwayRecord in airwayRecordDict.Values)
            {
                if (waypointRecordDict.TryGetValue(airwayRecord.toWaypointID, out toWaypoint))
                {
                    if (outgoingAirwaysByWaypointID.TryGetValue(airwayRecord.fromWaypointID, out List<(WaypointRecord, AirwayRecord)> connections))
                    {
                        outgoingAirwaysByWaypointID[airwayRecord.fromWaypointID].Add(
                            ((WaypointRecord)waypointRecordDict[airwayRecord.toWaypointID],
                            airwayRecord));
                    }
                    else
                    {
                        if (waypointRecordDict.TryGetValue(airwayRecord.toWaypointID, out toWaypoint))
                        {
                            outgoingAirwaysByWaypointID.Add(airwayRecord.fromWaypointID, new List<(WaypointRecord, AirwayRecord)> {
                        ((WaypointRecord)toWaypoint, airwayRecord)});
                        }
                    }
                }
            }

            Initialised = true;
        }

        public static WaypointRecord FindWaypointByIdent(string inputIdent)
        {
            foreach (WaypointRecord waypointRecord in waypointRecordDict.Values)
            {
                if (waypointRecord.ident == inputIdent)
                {
                    return waypointRecord;
                }
            }

            throw new WaypointNotFoundByIdentException();
        }

        public static AirportRecord FindAirportByIdent(string inputIdent)
        {
            foreach (AirportRecord airportRecord in airportRecordDict.Values)
            {
                if (airportRecord.ident == inputIdent)
                {
                    return airportRecord;
                }
            }

            throw new AirportNotFoundByIdentException();
        }

        public static AirwayRecord FindAirwayByName(string inputName)
        {
            foreach (AirwayRecord airwayRecord in airwayRecordDict.Values)
            {
                if (airwayRecord.airwayName == inputName)
                {
                    return airwayRecord;
                }
            }

            throw new AirwayNotFoundByIdentException();
        }
    }
}
