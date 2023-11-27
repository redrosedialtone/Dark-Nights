using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarkNights
{
    public class Chunk
    {
        public static (int X, int Y) Size => (Defs.ChunkSize, Defs.ChunkSize);
        public Coordinates Origin { get; set; }

        public Chunk(Coordinates Origin)
        {
            this.Origin = Origin;
        }


        public Coordinates Min => Origin;
        public Coordinates Max => new(Origin.X + Size.X, Origin.Y + Size.Y);

        public IEnumerable<Coordinates> Tiles()
        {
            for (int x = Min.X; x < Max.X; x++)
            {
                for (int y = Min.Y; y < Max.Y; y++)
                {
                    yield return new Coordinates(x, y);
                }
            }
        }

        public override int GetHashCode() =>
         Origin.X * 666 + Origin.Y * 1337;

        public static int GetHashCode(Coordinates Coordinates) =>
            Coordinates.X * 666 + Coordinates.Y * 1337;
    }
}
