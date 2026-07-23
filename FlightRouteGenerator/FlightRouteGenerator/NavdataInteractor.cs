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
        private static string NAV_DB_FILE_PATH = $"Data Source=\"{Directory.GetCurrentDirectory()}\\Data\\navdata.sqlite\";";
        private static SQLiteConnection navDBConnection = new SQLiteConnection(NAV_DB_FILE_PATH);
        public static Dictionary<string, Record> waypointRecordDict { get; private set; }
        public static Dictionary<string, Record> airportRecordDict { get; private set; }
        public static Dictionary<string, Record> airwayRecordDict { get; private set; }
        public static HashSet<string> VORIdentHashSet { get; private set; }
        public static HashSet<string> NDBIdentHashSet { get; private set; }
        public static Dictionary<string, List<(WaypointRecord, AirwayRecord)>> outgoingAirwaysByWaypointID { get; set; }
        private static HashSet<string> connectedWaypointIDs = new HashSet<string>();
        public static bool Initialised { get; private set; }

        private static Dictionary<string, Record> LoadRecords(string typeOfRecord)
        {
            Dictionary<string, Record> recordDict = new Dictionary<string, Record>();
            navDBConnection.Open();
            string command = "";
            switch (typeOfRecord)
            {
                case "waypoint":
                    command =
                @"SELECT waypoint_id, ident, lonx, laty, name
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
                    command = @"SELECT ident FROM vor";
                    break;

                case "ndb":
                    command = @"SELECT ident FROM ndb";
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

                        recordDict.Add(wpRecord.WaypointID, wpRecord);
                        break;

                    case "airport":
                        AirportRecord apRecord = new AirportRecord();

                        apRecord.AirportID = Convert.ToString(dataReader["airport_id"]);
                        apRecord.ident = (string)dataReader["ident"];
                        apRecord.name = (string)dataReader["name"];
                        apRecord.laty = Convert.ToDouble(dataReader["laty"]);
                        apRecord.lonx = Convert.ToDouble(dataReader["lonx"]);
                        apRecord.altitude = (int)(Convert.ToInt32(dataReader["altitude"]) / 3.281);
                        // converting altitude from feet to metres

                        recordDict.Add(apRecord.AirportID, apRecord);
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

                        recordDict.Add(awRecord.AirwayID, awRecord);
                        break;

                    case "vor":
                        VORIdentHashSet.Add((string)dataReader["ident"]);
                        break;

                    case "ndb":
                        NDBIdentHashSet.Add((string)dataReader["ident"]);
                        break;
                }
            }

            navDBConnection.Close();

            return recordDict;
        }

        public static Dictionary<string, Record> LoadWaypointRecords()
        {
            return LoadRecords("waypoint");
        }

        public static Dictionary<string, Record> LoadAirportRecords()
        {
            return LoadRecords("airport");
        }

        public static Dictionary<string, Record> LoadAirwayRecords()
        {
            return LoadRecords("airway");
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
            waypointRecordDict = new Dictionary<string, Record>();
            airportRecordDict = new Dictionary<string, Record>();
            airwayRecordDict = new Dictionary<string, Record>();
            VORIdentHashSet = new HashSet<string>();
            NDBIdentHashSet = new HashSet<string>();

            waypointRecordDict = LoadWaypointRecords();
            airportRecordDict = LoadAirportRecords();
            airwayRecordDict = LoadAirwayRecords();
            LoadVORIdentHashSet();
            LoadNDBIdentHashSet();

            foreach (Record record in waypointRecordDict.Values)
            {
                WaypointRecord wpRecord = (WaypointRecord)record;
                if (!connectedWaypointIDs.Contains(wpRecord.WaypointID))
                {
                    waypointRecordDict.Remove(wpRecord.WaypointID);
                }

                if (VORIdentHashSet.Contains(wpRecord.ident))
                {
                    wpRecord.Type = (int)WaypointType.VOR;
                }
                else if (NDBIdentHashSet.Contains(wpRecord.ident))
                {
                    wpRecord.Type = (int)WaypointType.NDB;
                }
                else
                {
                    wpRecord.Type = (int)WaypointType.NamedFix;
                }
            }

            outgoingAirwaysByWaypointID = new Dictionary<string, List<(WaypointRecord, AirwayRecord)>>();
            Record toWaypoint = new Record();

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
