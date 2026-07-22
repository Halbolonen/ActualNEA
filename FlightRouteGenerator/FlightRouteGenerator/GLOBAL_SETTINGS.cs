using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlightRouteGenerator
{
    internal class GLOBAL_SETTINGS
    {
        public static string DIRECT_FORMAT = "DCT";
        public static string FMS_DIRECT_FORMAT = "DRCT";
        public static string AIRAC_CYCLE = "2602";
        public static string DEST_ARPRT_WPT_ID = Convert.ToString(int.MaxValue);
    }
}
