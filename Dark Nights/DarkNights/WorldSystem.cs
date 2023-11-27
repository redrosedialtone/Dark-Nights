using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nebula;
using Nebula.Base;
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
        public LoggingLevel LoggingLevel { get => _loggingLevel; set => _loggingLevel = value; }
        private LoggingLevel _loggingLevel = LoggingLevel.Warn;

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
            ApplicationController.AddGizmo(gizmo);
            gizmo.SetDrawGizmo(true);
            gizmo.DrawChunks = true;
            gizmo.DrawTiles = true;
        }
    }

    public class WorldGizmo : IDrawGizmos
    {
        public bool DrawGizmo { get; private set; }
        public bool DrawChunks { get { return _drawChunks; } set { _drawChunks = value; SetDrawChunks(); } }
        private bool _drawChunks = false;
        public bool DrawTiles { get { return _drawTiles; } set { _drawTiles = value; SetDrawTiles(); } }
        private bool _drawTiles = false;

        private Color chunkGraphColor => new Color(75, 75, 225, 75);
        private DrawUtil chunkDrawCall;
        private Color tileGraphColor => new Color(75,75,75,75);
        private DrawUtil tileDrawCall;

        public WorldGizmo()
        {

        }

        public void DrawGizmos(SpriteBatch Batch)
        {
            if (DrawGizmo)
            {
                if (_drawChunks)
                {
                    
                }
            }
        }

        public void SetDrawGizmo(bool drawGizmo)
        {
            this.DrawGizmo = drawGizmo;
        }

        private void SetDrawChunks()
        {
            if (_drawChunks)
            {
                chunkDrawCall = null;
                foreach (var chunk in WorldSystem.Get.World.Chunks())
                {
                    Vector2[] corners = new Vector2[4];

                    corners[0] = new Coordinates(0, 0);
                    corners[1] = new Coordinates(Chunk.Size.X, 0);
                    corners[2] = new Coordinates(Chunk.Size.X, Chunk.Size.Y);
                    corners[3] = new Coordinates(0, Chunk.Size.Y);

                    var polygon = new Polygon(corners, chunk.Origin);
                    chunkDrawCall += DrawUtils.DrawPolygon(polygon, chunkGraphColor, 2f, drawType:DrawType.World);
                }
            }
            else
            {
                if (chunkDrawCall != null)
                {
                    DrawUtils.RemoveUtil(chunkDrawCall);
                    chunkDrawCall = null;
                }
            }
        }

        private void SetDrawTiles()
        {
            if (_drawTiles)
            {
                tileDrawCall = null;
                foreach (var chunk in WorldSystem.Get.World.Chunks())
                {
                    foreach (var tile in chunk.Tiles())
                    {
                        Vector2[] corners = new Vector2[4];

                        corners[0] = new Coordinates(0, 0);
                        corners[1] = new Coordinates(1, 0);
                        corners[2] = new Coordinates(1, 1);
                        corners[3] = new Coordinates(0, 1);

                        var polygon = new Polygon(corners, tile);
                        tileDrawCall += DrawUtils.DrawPolygon(polygon, tileGraphColor, 1f, drawType: DrawType.World);
                    }

                }
            }
            else
            {
                if (tileDrawCall != null)
                {
                    DrawUtils.RemoveUtil(tileDrawCall);
                    tileDrawCall = null;
                }
            }
        }
    }
}
