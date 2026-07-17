using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlightRouteGenerator
{
    internal class DegMinSecOrdinate
    {
        private double degrees;
        private double minutes;
        private double seconds;
        public double Degrees
        {
            get
            {
                return degrees;
            }
            set
            {
                degrees = value;
            }
        }
        public double Minutes
        {
            get
            {
                return minutes;
            }
            set
            {
                if (value == 60)
                {
                    minutes = 0;
                    Degrees++;
                }
                else
                {
                    minutes = value;
                }
            }
        }
        public double Seconds
        {
            get
            {
                return seconds;
            }
            set
            {
                if (value == 60)
                {
                    seconds = 0;
                    Minutes++;
                }
                else
                {
                    seconds = value;
                }
            }
        }
    }
}
