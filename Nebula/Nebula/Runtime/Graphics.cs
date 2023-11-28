﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Nebula.Base;
using Nebula.Runtime;

namespace Nebula.Main
{
    public interface ISpriteBatchDraw
    {
        void Draw(SpriteBatch Batch);
    }

    public class Graphics : IControl
    {
        private static readonly NLog.Logger log = NLog.LogManager.GetLogger("GRAPHICS");
        public static Graphics Access;

        public static SpriteBatch SpriteBatch => Access._spriteBatch;

        public GraphicsDeviceManager GraphicsDeviceMngr => NebulaRuntime.GraphicsDeviceMgr;
        public GraphicsDevice GraphicsDevice => NebulaRuntime.GraphicsDevice;
        public static int SCREEN_WIDTH = (int)(GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width);
        public static int SCREEN_HEIGHT = (int)(GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height);
        public static int SCREEN_ASPECT => SCREEN_WIDTH / SCREEN_HEIGHT;
        public static int RENDER_WIDTH => 1366;
        public static int RENDER_HEIGHT => 768;
        public static int RENDER_ASPECT => RENDER_WIDTH / RENDER_HEIGHT;

        private List<IDrawGizmos> gizmos = new List<IDrawGizmos>();
        private List<IDrawGizmos> worldGizmos = new List<IDrawGizmos>();
        private List<IDrawUIBatch> UIDrawCalls = new List<IDrawUIBatch>();
        private List<ISpriteBatchDraw> spriteBatchCalls = new List<ISpriteBatchDraw>();
        private Stack<SpriteBatchRenderer> textureBuffer = new Stack<SpriteBatchRenderer>();

        private NebulaRuntime RUNTIME;
        private SpriteBatch _spriteBatch;
        private RenderTarget2D renderTarget;

        //Texture2D ballTexture;


        public void Create(NebulaRuntime game)
        {
            RUNTIME = game;
            Access = this;
        }

        public void Initialise()
        {
            log.Info("> ..");
            //Graphics.IsFullScreen = true;
            GraphicsDeviceMngr.PreferredBackBufferWidth = RENDER_WIDTH;
            GraphicsDeviceMngr.PreferredBackBufferHeight = RENDER_HEIGHT;
            GraphicsDeviceMngr.ApplyChanges();
            renderTarget = new RenderTarget2D(GraphicsDevice, RENDER_WIDTH, RENDER_HEIGHT, false, SurfaceFormat.Color, DepthFormat.None);
        }

        private Polygon ourTestPolygon;

        public void LoadContent()
        {
            _spriteBatch = new SpriteBatch(NebulaRuntime.GraphicsDevice);

            DrawUtils.Setup(_spriteBatch);
            //ballTexture = RUNTIME.Content.Load<Texture2D>("DesignButtonLogo");

            Vector2[] points = new Vector2[]
            {
                new Vector2(0,25),
                new Vector2(25,25),
                new Vector2(25,0),
                new Vector2(0,0)
            };

            ourTestPolygon = new Polygon(points, new Vector2(500,500));

            DrawUtils.DrawPolygonOutline(ourTestPolygon, Color.White);
        }

        public void Update(GameTime gameTime)
        {
            ourTestPolygon.Position = new Vector2(ourTestPolygon.Position.X + 0.1f, ourTestPolygon.Position.Y + 0.1f);
        }

        public void Draw(GameTime gameTime)
        {
            GraphicsDevice.SetRenderTarget(renderTarget);
            GraphicsDevice.Clear(ClearOptions.Target, new Color(23,23,23), 1.0f, 0);



            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied);
            Cursor.Get.DrawCursor(_spriteBatch);
            DrawGizmos();
            DrawUtils.DrawGizmoBuffer(_spriteBatch);
            DrawUICalls(_spriteBatch);
            _spriteBatch.End();

            var matrix = Camera.Get.ViewTransformationMatrix;
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, matrix);
            log.Trace($"Drawing {spriteBatchCalls.Count} sprites..");
            DrawUtils.DrawWorldBuffer(_spriteBatch);
            DrawSpriteBatch(_spriteBatch);
            DrawWorldGizmos();
            _spriteBatch.End();

            GraphicsDevice.SetRenderTarget(null);

            _spriteBatch.Begin();
            _spriteBatch.Draw(renderTarget, new Rectangle(0,0,RENDER_WIDTH,RENDER_HEIGHT), Color.White);
            _spriteBatch.End();
        }

        public static void AddGizmo(IDrawGizmos Gizmo)
        {
            Access.gizmos.Add(Gizmo);
        }

        public static void AddWorldGizmo(IDrawGizmos Gizmo)
        {
            Access.worldGizmos.Add(Gizmo);
        }

        private void DrawGizmos()
        {
            foreach (var gizmo in gizmos)
            {
                gizmo.DrawGizmos(_spriteBatch);
            }
        }

        private void DrawWorldGizmos()
        {
            foreach (var gizmo in worldGizmos)
            {
                gizmo.DrawGizmos(_spriteBatch);
            }
        }

        public static void AddUIDraw(IDrawUIBatch Batch)
        {
            Access.UIDrawCalls.Add(Batch);
        }

        private void DrawUICalls(SpriteBatch Batch)
        {
            foreach (var batch in UIDrawCalls)
            {
                batch.DrawUI(Batch);
            }
        }

        public static void AddBatchDraw(ISpriteBatchDraw draw)
        {
            Access.spriteBatchCalls.Add(draw);
        }

        private void DrawSpriteBatch(SpriteBatch Batch)
        {
            foreach (var child in spriteBatchCalls)
            {
                child.Draw(Batch);
            }
        }

        public static void DrawLine(SpriteBatch spriteBatch, Vector2 point1, Vector2 point2, Color color, float thickness = 1f, float layerDepth = 0)
        {
            var distance = Vector2.Distance(point1, point2);
            var angle = (float)Math.Atan2(point2.Y - point1.Y, point2.X - point1.X);
            DrawLine(spriteBatch, point1, distance, angle, color, thickness, layerDepth);
        }

        public static void DrawLine(SpriteBatch spriteBatch, Vector2 point, float length, float angle, Color color, float thickness = 1f, float layerDepth = 0)
        {
            Texture2D _texture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            _texture.SetData(new[] { Color.White });
            var origin = new Vector2(0f, 0.5f);
            var scale = new Vector2(length, thickness);
            spriteBatch.Draw(_texture, point, null, color, angle, origin, scale, SpriteEffects.None, layerDepth);
        }

        public static void DrawLines(SpriteBatch spriteBatch, Vector2[] points, Color color, float thickness = 1f, float layerDepth = 0)
        {
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 vert = points[i];
                Vector2 nextVert = points[(i + 1) % points.Length];
                DrawLine(spriteBatch, vert, nextVert, color, thickness, layerDepth);
            }
        }

        public void UnloadContent()
        {
            return;
        }
    }
}
