using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FlightRouteGenerator
{
    internal class Route
    {
        public List<RouteLeg> Legs { get; set; }
        public AirportRecord DepartureAirport { get; set; }
        public AirportRecord ArrivalAirport { get; set; }
        public double TotalDistance { get; set; }
        // in nautical miles
        public int enrouteWaypointCount { get; set; }
        public double CruiseAltitude { get; set; }
        // in feet
        public Aircraft Aircraft { get; set; }
        public LoadsheetInfo Loadsheet { get; set; }
        public PDS_TCorTDInfo TC_Info { get; set; }
        // information about the Top of Climb point
        public PDS_TCorTDInfo TD_Info { get; set; }
        // information about the Top of Descent point

        public string GetOneLineFormat()
        {
            string singleLineRoute = $"{DepartureAirport.ident} ";
            RouteLeg previousLeg = new RouteLeg();
            previousLeg.Airway = new AirwayRecord();
            previousLeg.Airway.isDirect = true;
            foreach (RouteLeg leg in Legs)
            {
                if (leg.Airway.AirwayID != previousLeg.Airway.AirwayID)
                {
                    if (previousLeg.Airway.isDirect || leg.isAirportLeg)
                    {
                        singleLineRoute += $"{GLOBAL_SETTINGS.DIRECT_FORMAT} ";
                    }
                    else
                    {
                        singleLineRoute += $"{previousLeg.Airway.airwayName} ";
                    }
                    singleLineRoute += $"{leg.Waypoint.ident} ";
                    previousLeg = leg;
                }
            }

            return singleLineRoute;
        }

        public Route()
        {
            enrouteWaypointCount = 0;
        }
    }
}
