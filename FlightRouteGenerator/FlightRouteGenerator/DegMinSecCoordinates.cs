using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlightRouteGenerator
{
    internal class DegMinSecCoordinates
    {
        public DegMinSecOrdinate Latitude { get; set; }
        public DegMinSecOrdinate Longitude { get; set; }
        public bool LatitudeIsSouth { get; set; }
        public bool LongitudeIsWest { get; set; }

        private static string GetOrdinateStringSegmentAppendedString(DegMinSecOrdinate ordinate, string toPrint)
        {
            toPrint += $"{ordinate.Degrees}° ";
            toPrint += $"{ordinate.Minutes}' ";
            toPrint += $"{ordinate.Seconds:F2}\"";

            return toPrint;
        }

        public string GetPrintableString()
        {
            string toPrint = "";
            if (LatitudeIsSouth)
            {
                toPrint += "S";
            }
            else
            {
                toPrint += "N";
            }

            toPrint = GetOrdinateStringSegmentAppendedString(Latitude, toPrint) + ",";
            
            if (LongitudeIsWest)
            {
                toPrint += "W";
            }
            else
            {
                toPrint += "E";
            }

            toPrint = GetOrdinateStringSegmentAppendedString(Longitude, toPrint);

            return toPrint;
        }
    }
}
