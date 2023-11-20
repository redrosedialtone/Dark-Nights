using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Content
{
    public interface ICoordinate
    {
        int X { get; }
        int Y { get; }
    }

    public struct Coordinate : ICoordinate
    {
        public int X { get; set; }
        public int Y { get; set; }

        public Coordinate(int x, int y)
        {
            this.X = x; this.Y = y;
        }

        public Coordinate(float x, float y)
            : this((int)MathF.Floor(x), (int)MathF.Floor(y)) { }
        public Coordinate(Point point)
            : this(point.X, point.Y) { }
        public override string ToString()
        {
            return String.Format("[{0},{1}]",X,Y);
        }
    }
}
