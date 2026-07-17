using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlightRouteGenerator
{
    internal class ConvertCoordinates
    {
        public static DegMinSecCoordinates DecimalToDegMinSec(double decimalLaty, double decimalLonx)
        {
            bool latyIsSouth = decimalLaty < 0;
            bool lonxIsWest = decimalLonx < 0;

            DegMinSecCoordinates dmsCoords = new DegMinSecCoordinates();
        }
    }
}
