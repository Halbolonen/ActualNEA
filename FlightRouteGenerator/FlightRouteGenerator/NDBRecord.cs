using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlightRouteGenerator
{
    internal class NDBRecord : Record
    {
        public string NDB_ID
        {
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
