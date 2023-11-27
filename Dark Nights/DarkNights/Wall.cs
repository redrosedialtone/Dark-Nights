using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarkNights
{
    public class Wall
    {
        public Coordinates Coordinates { get; set; }

        public Wall(Coordinates Coordinates)
        {
            this.Coordinates = Coordinates;
        }
    }
}
