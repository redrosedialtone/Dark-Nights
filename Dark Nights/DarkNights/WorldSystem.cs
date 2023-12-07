using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nebula;
using Nebula.Main;
using Nebula.Runtime;
using Nebula.Systems;
using static Nebula.Runtime.DrawUtils;

namespace DarkNights
{
    public class WorldSystem : Manager
    {
        #region Static
        private static WorldSystem instance;
        public static WorldSystem Get => instance;

        public static readonly NLog.Logger log = NLog.LogManager.GetLogger("WORLD");

        public List<string> Logs { get; set; } = new List<string>();

        #endregion
        public World World;

        private WorldGizmo gizmo;

        public override void Init()
        {
            log.Info("> ...");
            instance = this;
            World = new World(0, 5, 5);
            World.GenerateChunks();

            ApplicationController.Get.Initiate(this);
        }

        public override void OnInitialized()
        {
            base.OnInitialized();
            gizmo = new WorldGizmo();
            gizmo.Enabled = true;
            gizmo.DrawChunks = true;
            gizmo.DrawTiles = true;
            gizmo.DrawChunksUnderMouse = true;
        }

        public IEnumerable<Coordinates> TilesInRadius(Vector2 position, float radius)
        {
            Coordinates min = new Coordinates((int)MathF.Floor(position.X - radius), (int)MathF.Floor(position.Y - radius));
            Coordinates max = new Coordinates((int)MathF.Floor(position.X + radius), (int)MathF.Floor(position.Y + radius));

            for (int x = min.X; x < max.X; x++)
            {
                for (int y = min.Y; y < max.Y; y++)
                {
                    yield return new Coordinates(x, y);
                }
            } 
        }

        public IEnumerable<Chunk> ChunksInRadius(Vector2 position, float tiles)
        {
            //tiles *= Defs.UnitPixelSize;
            Vector2 min = new Vector2(position.X - tiles, position.Y - tiles);
            Vector2 max = new Vector2(position.X + tiles, position.Y + tiles);
            Coordinates tileMin = new Coordinates(min);
            Coordinates tileMax = new Coordinates(max);

            Chunk chunkMin = Chunk.Get(tileMin);
            Chunk chunkMax = Chunk.Get(tileMax);

            if (chunkMin == null || chunkMax == null) yield break;

            for (int x = chunkMin.ChunkCoordinates.X; x <= chunkMax.ChunkCoordinates.X; x++)
            {
                for (int y = chunkMin.ChunkCoordinates.Y; y <= chunkMax.ChunkCoordinates.Y; y++)
                {
                    yield return World.ChunkUnsf(new Coordinates(x, y));
                }
            }
        }
    }

    public class WorldGizmo : IGizmo
    {
        public bool Enabled { get; set; }
        public bool DrawChunks { get { return _drawChunks; } set { _drawChunks = value; SetDrawChunks(); } }
        private bool _drawChunks = false;
        public bool DrawChunksUnderMouse { get { return _drawChunksUnderMouse; } set { _drawChunksUnderMouse = value; } }
        private bool _drawChunksUnderMouse = false;
        private float drawChunksUnderMouseRadius = 1.0f;
        public bool DrawTiles { get { return _drawTiles; } set { _drawTiles = value; SetDrawTiles(); } }
        private bool _drawTiles = false;

        private Color chunkGraphColor => new Color(75, 75, 225, 75);
        private Polygon[] chunkPolys;
        private Color tileGraphColor => new Color(75,75,75,75);
        private Polygon[] tilePolys;
        private Color chunkHighlightColor => new Color(225,75,225,225);

        public WorldGizmo()
        {
            Debug.NewWorldGizmo(this);
        }

        public void Update() { }
        
        public void Draw()
        {
            if (_drawChunks)
            {
                foreach (var chunk in chunkPolys)
                {
                    DrawUtils.DrawPolygonOutlineToWorld(chunk, chunkGraphColor, 2.0f);
                }
            }
            if (_drawTiles)
            {
                foreach (var tile in tilePolys)
                {
                    DrawUtils.DrawPolygonOutlineToWorld(tile, tileGraphColor, 1.0f);
                }
            }
            if (_drawChunksUnderMouse)
            {
                Vector2 pos = Camera.ScreenToWorld(Cursor.Position);
                Chunk chunk = Chunk.Get(pos);
                if (chunk == null) return;
                Vector2[] corners = new Vector2[4];

                corners[0] = new Coordinates(0, 0);
                corners[1] = new Coordinates(Chunk.Size.X, 0);
                corners[2] = new Coordinates(Chunk.Size.X, Chunk.Size.Y);
                corners[3] = new Coordinates(0, Chunk.Size.Y);

                var polygon = new Polygon(corners, chunk.Origin);

                DrawUtils.DrawPolygonOutlineToWorld(polygon, chunkHighlightColor, 5.0f);
                
                /*foreach (var chunk in WorldSystem.Get.ChunksInRadius(pos, drawChunksUnderMouseRadius))
                {
                    if (chunk == null) continue;
                    Vector2[] corners = new Vector2[4];

                    corners[0] = new Coordinates(0, 0);
                    corners[1] = new Coordinates(Chunk.Size.X, 0);
                    corners[2] = new Coordinates(Chunk.Size.X, Chunk.Size.Y);
                    corners[3] = new Coordinates(0, Chunk.Size.Y);

                    var polygon = new Polygon(corners, chunk.Origin);

                    DrawUtils.DrawPolygonOutlineToWorld(polygon, chunkHighlightColor,5.0f);
                }*/
            }
        }

        private void SetDrawChunks()
        {
            if (_drawChunks)
            {
                List<Polygon> polys = new List<Polygon>();
                foreach (var chunk in WorldSystem.Get.World.Chunks())
                {
                    Vector2[] corners = new Vector2[4];

                    corners[0] = new Coordinates(0, 0);
                    corners[1] = new Coordinates(Chunk.Size.X, 0);
                    corners[2] = new Coordinates(Chunk.Size.X, Chunk.Size.Y);
                    corners[3] = new Coordinates(0, Chunk.Size.Y);

                    var polygon = new Polygon(corners, chunk.Origin);
                    polys.Add(polygon);
                }
                chunkPolys = polys.ToArray();
            }
            else
            {
                chunkPolys = null;
            }
        }

        private void SetDrawTiles()
        {
            if (_drawTiles)
            {
                List<Polygon> polys = new List<Polygon>();
                foreach (var tile in WorldSystem.Get.World.Tiles())
                {
                    Vector2[] corners = new Vector2[4];

                    corners[0] = new Coordinates(0, 0);
                    corners[1] = new Coordinates(1, 0);
                    corners[2] = new Coordinates(1, 1);
                    corners[3] = new Coordinates(0, 1);

                    var polygon = new Polygon(corners, tile);
                    polys.Add(polygon);
                }
                tilePolys = polys.ToArray();
            }
            else
            {
                tilePolys = null;
            }
        }
    }
}
