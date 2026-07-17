using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlightRouteGenerator
{
    internal class ConvertCoordinates
    {
        private static DegMinSecOrdinate ConvertDecimalOrdinateToDMS(double decimalOrdinate)
        {
            DegMinSecOrdinate resultOrdinate = new DegMinSecOrdinate();

            resultOrdinate.Degrees = Math.Floor(Math.Abs(decimalOrdinate));
            double minutesDecimalPart = (Math.Abs(decimalOrdinate) - resultOrdinate.Degrees) * 60;
            resultOrdinate.Minutes = Math.Floor(minutesDecimalPart);
            resultOrdinate.Seconds = Math.Round((minutesDecimalPart - resultOrdinate.Minutes) * 60, 2);

            return resultOrdinate;
        }
        
        public static DegMinSecCoordinates DecimalToDegMinSec(double decimalLaty, double decimalLonx)
        {
            DegMinSecCoordinates dmsCoords = new DegMinSecCoordinates();

            dmsCoords.LatitudeIsSouth = decimalLaty < 0;
            dmsCoords.LongitudeIsWest = decimalLonx < 0;

            dmsCoords.Latitude = ConvertDecimalOrdinateToDMS(Math.Abs(decimalLaty));
            dmsCoords.Longitude = ConvertDecimalOrdinateToDMS(Math.Abs(decimalLonx));


            return dmsCoords;
        }
    }
}
