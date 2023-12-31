using Microsoft.Xna.Framework;
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
        public readonly ImpassableNode Node;

        public Wall(Coordinates Coordinates)
        {
            this.Coordinates = Coordinates;
            Node = new ImpassableNode(Coordinates);
        }
    }
}
