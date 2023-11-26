using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Runtime
{
    public class Polygon
    {
        public Vector2[] Points;
        public Vector2 Position;

        public Polygon(Vector2[] Points, Vector2 position)
        {
            this.Points = Points; this.Position = position;
        }
    }
}
