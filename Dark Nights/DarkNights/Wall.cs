using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarkNights
{
    public class Wall : INavNode
    {
        public PassabilityFlags Passability => PassabilityFlags.Impassable;
        public Vector2 Position => Coordinates;
        public Coordinates Coordinates { get; set; }
        public float Cost => 1.0f;

        public Wall(Coordinates Coordinates)
        {
            this.Coordinates = Coordinates;
        }
    }
}
