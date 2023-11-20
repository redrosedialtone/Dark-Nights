using Nebula;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarkNights
{
    public class World
    {
        #region Static
        private static readonly NLog.Logger log = NLog.LogManager.GetLogger("WORLD");

        public List<string> Logs { get; set; } = new List<string>();
        public LoggingLevel LoggingLevel { get => _loggingLevel; set => _loggingLevel = value; }
        private LoggingLevel _loggingLevel = LoggingLevel.Warn;

        #endregion

        public static int SEED;
        public (int X, int Y) Size => ((Maximum.X - Minimum.X), (Maximum.Y - Minimum.Y));
        public (int X, int Y) Minimum;
        public (int X, int Y) Maximum;

        private readonly Dictionary<int, Chunk> Chunks;

        public World(int seed, int width, int height)
        {
            SEED = seed;
            Chunks = new Dictionary<int, Chunk>();
            float _halfWidth = width / 2;
            float _halfHeight = height / 2;

            Minimum = ((int)MathF.Floor(0 - _halfWidth), (int)MathF.Floor(0 - _halfHeight));
            Maximum = ((int)MathF.Ceiling(_halfWidth), (int)MathF.Ceiling(_halfHeight));
        }

        public void GenerateChunks()
        {
            log.Info($"> In the first moments, Nebula created a world {Size.X * Size.Y * WorldSystem.CHUNK_SIZE} tiles large... ");
            for (int x = Minimum.X; x < Maximum.X; x++)
            {
                for (int y = Minimum.Y; y < Maximum.Y; y++)
                {
                    CreateChunk(x, y);
                }
            }
        }

        public void CreateChunk(int X, int Y)
        {
            log.Trace($"Creating Chunk @ ({X},{Y})");
            Chunk newChunk = new Chunk();

        }
    }
}
