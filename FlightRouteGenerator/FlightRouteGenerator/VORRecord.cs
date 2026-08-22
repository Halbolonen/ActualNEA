using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlightRouteGenerator
{
    internal class VORRecord : Record
    {
        public string VOR_ID {
            get
            {
                return primaryKey;
            }
            set
            {
                primaryKey = value;
            }
        }
        public string ident { get; set; }
        public string name { get; set; }
    }
}
