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

    public class Circle
    {
        public Vector2 Centre;
        public float Radius;

        public Circle(Vector2 Centre, float Radius)
        {
            this.Centre = Centre; this.Radius = Radius;
        }
    }

    public class Line
    {
        public Vector2 From;
        public Vector2 To;

        public Line(Vector2 From, Vector2 To)
        {
            this.From = From; this.To = To;
        }
    }
}
