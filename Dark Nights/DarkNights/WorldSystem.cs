using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
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
            World = new World("test".GetHashCode(), 14, 14);
            World.GenerateChunks();
 
            ApplicationController.Get.Initiate(this);
        }

        public override void OnInitialized()
        {
            base.OnInitialized();
            gizmo = new WorldGizmo();
            gizmo.Enabled = true;
            //gizmo.DrawChunks = true;
            gizmo.DrawTiles = true;
            gizmo.DrawChunksUnderMouse = true;
            gizmo.drawNoise = true;

            World.GenerateBiomeData();
        }

        public Coordinates ClampToWorld(Coordinates Coordinate)
        {
            int x = (int)MathF.Min(MathF.Max(Coordinate.X, World.MinimumTile.X), World.MaximumTile.X);
            int y = (int)MathF.Min(MathF.Max(Coordinate.Y, World.MinimumTile.Y), World.MaximumTile.Y);
            return new Coordinates(x, y);
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
        public bool DrawTiles { get { return _drawTiles; } set { _drawTiles = value; } }
        private bool _drawTiles = false;
        public bool drawNoise = false;

        private Color chunkGraphColor => new Color(75, 75, 225, 75);
        private Polygon[] chunkPolys;
        private Color tileGraphColor => new Color(75,75,75,75);
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
                Rectangle viewport = Camera.Get.ViewportWorldBoundry();

                Coordinates min = WorldSystem.Get.ClampToWorld(new Vector2(viewport.X, viewport.Y));
                Coordinates max = WorldSystem.Get.ClampToWorld(new Vector2(viewport.X + viewport.Width, viewport.Y + viewport.Height));
                Vector2 delta = new Coordinates(max.X - min.X, max.Y - min.Y);

                for (int x = min.X; x < max.X; x++)
                {
                    Vector2 pos = new Coordinates(x, min.Y);
                    Rectangle xRect = new Rectangle((int)pos.X,(int)pos.Y, 1, (int)delta.Y);
                    DrawUtils.DrawPolygonOutlineToWorld(xRect, tileGraphColor);
                }
                for (int y = min.Y; y < max.Y; y++)
                {
                    Vector2 pos = new Coordinates(min.X, y);
                    Rectangle yRect = new Rectangle((int)pos.X, (int)pos.Y, (int)delta.X, 1);
                    DrawUtils.DrawPolygonOutlineToWorld(yRect, tileGraphColor); 
                    /*Vector2 tile = new Coordinates(x, y);
                    Rectangle rect = new Rectangle((int)tile.X, (int)tile.Y, Defs.UnitPixelSize, Defs.UnitPixelSize);
                    DrawUtils.DrawPolygonOutlineToWorld(rect, tileGraphColor);*/
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
            if (drawNoise)
            {
                int i = 0;
                int minX = 0 - WorldSystem.Get.World.Minimum.X;
                int minY = 0 - WorldSystem.Get.World.Minimum.Y;
                foreach (var chunk in WorldSystem.Get.World.Chunks())
                {
                    float val;
                    if (WorldSystem.Get.World.FertilityMap.TryGetValue(chunk.GetHashCode(), out val))
                    {
                        float grad = 1.0f / 1.0f * (val);
                        var colour = Color.Lerp(Color.Transparent, Color.White, grad);
                        DrawUtils.DrawRectangleToWorld(chunk.Origin,
                            Defs.UnitPixelSize * World.CHUNK_SIZE,
                            Defs.UnitPixelSize * World.CHUNK_SIZE,
                            colour);
                        i++;
                    }
                }
            }
        }

        private float colourGrad(float y) => 1 - 2 * MathF.Abs(y - 0.5f);

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
    }
}
