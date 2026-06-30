using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace FlightRouteGenerator
{
    public static class Navigator
    {
        public static double GetDistanceBetween(double orgLaty, double orgLonx, double dstLaty, double dstLonx)
        {
            double phi1 = orgLaty;
            double phi2 = dstLaty;
            double lambda1 = orgLonx;
            double lambda2 = dstLonx;
            const int r = 3444;
            // earth radius in nmi

            double h = Math.Pow(Math.Sin((phi2 - phi1) / 2), 2) + Math.Cos(phi1) * Math.Cos(phi2) * Math.Pow(Math.Sin((lambda2 - lambda1) / 2), 2);

            return 2 * r * Math.Pow(Math.Sin(Math.Sqrt(h)), -1);
        }
    }
}
