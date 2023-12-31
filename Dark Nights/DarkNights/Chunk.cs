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

        public Coordinates Min => Origin;
        public Coordinates Max => new(Origin.X + Size.X, Origin.Y + Size.Y);

        public Chunk(Coordinates Origin)
        {
            this.Origin = Origin;
        }

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

        public static Chunk Get(Coordinates Coordinates)
        {
            int chunkX = (int)MathF.Floor((float)Coordinates.X / Size.X);
            int chunkY = (int)MathF.Floor((float)Coordinates.Y / Size.Y);
            return WorldSystem.Get.World.ChunkUnsf(new Coordinates(chunkX, chunkY));
        }

        public Coordinates ChunkCoordinates
        {
            get
            {
                int chunkX = (int)MathF.Floor((float)Origin.X / Size.X);
                int chunkY = (int)MathF.Floor((float)Origin.Y / Size.Y);
                return new Coordinates(chunkX, chunkY);
            }
        }

        public override int GetHashCode() =>
         (Origin.X / Size.X) * 666 + (Origin.Y / Size.Y) * 1337;

        public static int GetHashCode(Coordinates Coordinates) =>
            Coordinates.X * 666 + Coordinates.Y * 1337;
    }
}
