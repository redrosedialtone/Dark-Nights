using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarkNights
{
    public struct Coordinates
    {
        public int X { get; set; }
        public int Y { get; set; }

        public Coordinates((int X, int Y) Coordinates)
            : this(Coordinates.X, Coordinates.Y) { }
        public Coordinates(Vector2 Coordinates)
            : this((int)MathF.Floor(Coordinates.X), (int)MathF.Floor(Coordinates.Y)) { }

        public Coordinates(int X, int Y)
        { this.X = X; this.Y = Y; }

        public static implicit operator Vector2(Coordinates Coordinate) =>
            new Vector2(Coordinate.X, Coordinate.Y);

        public static implicit operator Coordinates(Vector2 Coordinate) =>
            new Coordinates((int)MathF.Floor(Coordinate.X), (int)MathF.Floor(Coordinate.Y));

        public override string ToString()
        {
            return $"({X},{Y})";
        }

        public override int GetHashCode() =>
         this.X * 666 + this.Y * 1339;

        public override bool Equals(object Other)
        {
            if (Other != null && Other is Coordinates point)
            {
                return this.X == point.X && this.Y == point.Y;
            }
            return false;
        }

        public static Coordinates operator +(Coordinates a, Coordinates b) =>
            new Coordinates(a.X + b.X, a.Y + b.Y);

        public static bool operator ==(Coordinates a, Coordinates b) =>
            a.Equals(b);

        public static bool operator !=(Coordinates a, Coordinates b) =>
            !a.Equals(b);

        public static bool operator ==(Coordinates a, Vector2 b) =>
            a.X == b.X && a.Y == b.X;

        public static bool operator !=(Coordinates a, Vector2 b) =>
            a.X != b.X || a.Y != b.X;

        public static implicit operator Vector3(Coordinates v) =>
            new Vector3(v.X, v.Y,0);
    }
}
